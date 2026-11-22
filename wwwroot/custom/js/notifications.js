document.addEventListener('DOMContentLoaded', () => {
  const list = document.getElementById('notificationList');
  const unreadBadge = document.getElementById('unreadCount');
  const notificationDot = document.getElementById('notificationDot');
  const markAllButton = document.getElementById('markAllRead');

  if (!list) return;

  const API = '/api/notifications';

  // Format timestamp
  function timeAgo(dateString) {
    const date = new Date(dateString);
    const seconds = Math.floor((Date.now() - date.getTime()) / 1000);

    if (seconds < 60) return 'Just now';
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
    if (seconds < 604800) return `${Math.floor(seconds / 86400)}d ago`;

    return date.toLocaleDateString();
  }

  // Build notification element
  function createNotification(notification) {
    const item = document.createElement('li');

    item.className =
      'list-group-item list-group-item-action dropdown-notifications-item' +
      (notification.isRead ? ' marked-as-read' : '');

    const wrapper = document.createElement('div');
    wrapper.className = 'd-flex';

    // Icon
    const iconContainer = document.createElement('div');
    iconContainer.className = 'flex-shrink-0 me-3';

    const avatar = document.createElement('div');
    avatar.className = 'avatar';

    const icon = document.createElement('span');
    icon.className = 'avatar-initial rounded-circle ' + getNotificationColor(notification.type);

    const iconElement = document.createElement('i');
    iconElement.className = 'icon-base ' + getNotificationIcon(notification.type);

    icon.appendChild(iconElement);
    avatar.appendChild(icon);
    iconContainer.appendChild(avatar);

    // Content
    const content = document.createElement('div');
    content.className = 'flex-grow-1';

    const title = document.createElement('h6');
    title.className = 'small mb-50';
    title.textContent = notification.title;

    const message = document.createElement('small');
    message.className = 'mb-1 d-block text-body';
    message.textContent = notification.message;

    const time = document.createElement('small');
    time.className = 'text-body-secondary';
    time.textContent = timeAgo(notification.createdAt);

    content.append(title, message, time);

    // Read indicator
    const actions = document.createElement('div');
    actions.className = 'flex-shrink-0 dropdown-notifications-actions';

    if (!notification.isRead) {
      const readButton = document.createElement('a');
      readButton.href = 'javascript:void(0)';
      readButton.className = 'dropdown-notifications-read';
      readButton.title = 'Mark as read';

      const dot = document.createElement('span');
      dot.className = 'badge badge-dot';

      readButton.appendChild(dot);

      readButton.addEventListener('click', async event => {
        event.stopPropagation();
        await markAsRead(notification.id);
      });

      actions.appendChild(readButton);
    }

    wrapper.append(iconContainer, content, actions);
    item.appendChild(wrapper);

    // Clicking notification
    item.style.cursor = 'pointer';

    item.addEventListener('click', async () => {
      if (!notification.isRead) {
        await markAsRead(notification.id, false);
      }

      const url = notification.actionUrl;

      // Only allow local application paths
      if (url && url.startsWith('/') && !url.startsWith('//')) {
        window.location.href = url;
      }
    });

    return item;
  }

  function getNotificationColor(type) {
    switch (type?.toLowerCase()) {
      case 'success':
        return 'bg-label-success';
      case 'warning':
        return 'bg-label-warning';
      case 'error':
        return 'bg-label-danger';
      default:
        return 'bg-label-primary';
    }
  }

  function getNotificationIcon(type) {
    switch (type?.toLowerCase()) {
      case 'success':
        return 'ri ri-checkbox-circle-line';
      case 'warning':
        return 'ri ri-error-warning-line';
      case 'error':
        return 'ri ri-close-circle-line';
      default:
        return 'ri ri-information-line';
    }
  }

  // Fetch notifications
  async function loadNotifications() {
    try {
      const response = await fetch(API, {
        credentials: 'same-origin'
      });

      if (!response.ok) {
        throw new Error('Unable to fetch notifications');
      }

      const data = await response.json();

      // Update unread count
      unreadBadge.textContent = `${data.unreadCount} New`;

      notificationDot.style.display = data.unreadCount > 0 ? '' : 'none';

      // Render notifications
      list.replaceChildren();

      if (data.notifications.length === 0) {
        const empty = document.createElement('li');
        empty.className = 'list-group-item text-center text-body-secondary py-4';

        empty.textContent = "You're all caught up!";

        list.appendChild(empty);
        return;
      }

      data.notifications.forEach(notification => {
        list.appendChild(createNotification(notification));
      });
    } catch (error) {
      console.error('Notification polling error:', error);
    }
  }

  // Mark one notification as read
  async function markAsRead(id, reload = true) {
    try {
      const response = await fetch(`${API}/${id}/read`, {
        method: 'POST',
        credentials: 'same-origin'
      });

      if (!response.ok) {
        throw new Error('Unable to mark notification as read');
      }

      if (reload) {
        await loadNotifications();
      }
    } catch (error) {
      console.error(error);
    }
  }

  // Mark all as read
  markAllButton.addEventListener('click', async event => {
    event.preventDefault();

    try {
      const response = await fetch(`${API}/read-all`, {
        method: 'POST',
        credentials: 'same-origin'
      });

      if (!response.ok) {
        throw new Error('Unable to mark all as read');
      }

      await loadNotifications();
    } catch (error) {
      console.error(error);
    }
  });

  // Initial fetch
  loadNotifications();

  // Poll every 15 seconds
  setInterval(loadNotifications, 15000);
});

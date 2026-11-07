// Remove excess characters.
function sanitizeInput(url) {
  const regex = /\bedit\b/i;

  if (regex.test(url)) {
    Swal.fire({
      icon: 'warning',
      title: 'Warning!',
      text: 'Please enter a valid Google Form URL.',
      confirmButtonText: 'OK',
      confirmButtonColor: '#ff9800'
    });
    return '';
  }

  return url.replace(/\bviewform\b.*/i, 'viewform');
}

async function getOriginalUrl(input) {
  const url = input;
  const regex = /\bdocs.google.com\b/i;

  if (regex.test(url)) {
    return sanitizeInput(url);
  } else {
    // Get the original link.
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    const response = await fetch(window.appUrls.getOriginalUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        RequestVerificationToken: token
      },
      body: JSON.stringify({ url })
    });

    // If error, return the initial URL.
    if (!response.ok) throw new Error(`Request failed with status ${response.status}`);

    const original_url = await response.text();

    // Return the URL.
    return sanitizeInput(original_url);
  }
}

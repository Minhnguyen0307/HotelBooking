export async function api(path, options = {}) {
  const headers = new Headers(options.headers || {});
  const token = localStorage.getItem('token');
  if (token) headers.set('Authorization', `Bearer ${token}`);

  let body = options.body;
  if (body && !(body instanceof FormData) && typeof body !== 'string') {
    headers.set('Content-Type', 'application/json');
    body = JSON.stringify(body);
  }

  const response = await fetch(path.startsWith('/') ? path : `/api${path}`, {
    method: options.method || 'GET',
    headers,
    body
  });

  if (response.status === 204) return null;

  const text = await response.text();
  let data = null;
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = text;
    }
  }

  if (!response.ok) {
    const validation = data && data.errors
      ? Object.values(data.errors).flat().join(' ')
      : '';
    const error = new Error(
      (data && data.message) || validation || (data && data.title) || 'Yêu cầu thất bại.'
    );
    error.status = response.status;
    error.data = data;
    throw error;
  }

  return data;
}

export async function downloadFile(path, filename) {
  const token = localStorage.getItem('token');
  const response = await fetch(path, {
    headers: token ? { Authorization: `Bearer ${token}` } : {}
  });
  if (!response.ok) throw new Error('Không tải được tệp.');
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  link.click();
  URL.revokeObjectURL(url);
}

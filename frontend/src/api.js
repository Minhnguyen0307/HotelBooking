export async function api(path, options = {}) {
  const headers = new Headers(options.headers || {});
  const token = localStorage.getItem('token');
  if (token) headers.set('Authorization', `Bearer ${token}`);

  let body = options.body;
  if (body && !(body instanceof FormData) && typeof body !== 'string') {
    headers.set('Content-Type', 'application/json');
    body = JSON.stringify(body);
  }

  const method = options.method || 'GET';
  const retries = options.retries ?? (method === 'GET' ? 3 : 0);

  for (let attempt = 0; ; attempt += 1) {
    let response;
    try {
      response = await fetch(path.startsWith('/') ? path : `/api${path}`, {
        method,
        headers,
        body,
        signal: options.signal || AbortSignal.timeout(12000)
      });
    } catch (cause) {
      if (cause.name === 'AbortError') throw cause;
      const error = new Error(
        'Không kết nối được tới máy chủ API. Hãy khởi động backend trong Visual Studio rồi thử tải lại.'
      );
      error.code = cause.name === 'TimeoutError' ? 'API_TIMEOUT' : 'API_UNAVAILABLE';
      error.cause = cause;
      if (attempt < retries) {
        await new Promise((resolve) => setTimeout(resolve, 300 * (2 ** attempt)));
        continue;
      }
      throw error;
    }

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
      const validation = data && typeof data === 'object' && data.errors
        ? Object.values(data.errors).flat().join(' ')
        : '';
      const serverMessage = data && typeof data === 'object'
        ? data.message || validation || data.title
        : '';
      const message = serverMessage || (response.status >= 500
        ? `Máy chủ API gặp lỗi (HTTP ${response.status}). Hãy thử lại sau khi kiểm tra backend.`
        : (typeof data === 'string' && data.trim() && !data.trimStart().startsWith('<')
          ? data.slice(0, 240)
          : `Yêu cầu thất bại (HTTP ${response.status}).`));
      const error = new Error(message);
      error.status = response.status;
      error.data = data;
      if (response.status >= 500 && attempt < retries) {
        await new Promise((resolve) => setTimeout(resolve, 300 * (2 ** attempt)));
        continue;
      }
      throw error;
    }

    return data;
  }
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

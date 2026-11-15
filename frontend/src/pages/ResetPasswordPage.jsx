import { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { api } from '../api';

export function ResetPasswordPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const [error, setError] = useState('');

  async function onSubmit(event) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    setError('');
    try {
      const result = await api('/api/auth/reset-password', { method: 'POST', body: data });
      navigate('/login', { state: { message: result.message, kind: 'ok' } });
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <form className="panel stack account-card" onSubmit={onSubmit}>
      <h1>Đặt lại mật khẩu</h1>
      {error && <div className="banner">{error}</div>}
      <input type="hidden" name="email" value={params.get('email') || ''} />
      <input type="hidden" name="token" value={params.get('token') || ''} />
      <label>Mật khẩu mới<input name="newPassword" type="password" required /></label>
      <label>Nhập lại mật khẩu<input name="confirmNewPassword" type="password" required /></label>
      <button className="btn" type="submit">Lưu mật khẩu</button>
    </form>
  );
}

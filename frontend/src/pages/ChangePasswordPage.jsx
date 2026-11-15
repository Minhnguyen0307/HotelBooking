import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api';

export function ChangePasswordPage() {
  const navigate = useNavigate();
  const [error, setError] = useState('');

  async function onSubmit(event) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    setError('');
    try {
      const result = await api('/api/auth/change-password', { method: 'POST', body: data });
      navigate('/profile', { state: { message: result.message, kind: 'ok' } });
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <form className="panel stack account-card" onSubmit={onSubmit}>
      <h1>Đổi mật khẩu</h1>
      {error && <div className="banner">{error}</div>}
      <label>Mật khẩu hiện tại<input name="oldPassword" type="password" required /></label>
      <label>Mật khẩu mới<input name="newPassword" type="password" required /></label>
      <label>Nhập lại mật khẩu<input name="confirmNewPassword" type="password" required /></label>
      <button className="btn" type="submit">Cập nhật</button>
    </form>
  );
}

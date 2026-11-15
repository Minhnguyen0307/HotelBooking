import { useState } from 'react';
import { api } from '../api';

export function ForgotPasswordPage() {
  const [error, setError] = useState('');
  const [info, setInfo] = useState('');

  async function onSubmit(event) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    setError('');
    try {
      const result = await api('/api/auth/forgot-password', { method: 'POST', body: data });
      setInfo(result.resetLink ? `${result.message} Liên kết: ${result.resetLink}` : result.message);
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <form className="panel stack account-card" onSubmit={onSubmit}>
      <h1>Quên mật khẩu</h1>
      {error && <div className="banner">{error}</div>}
      {info && <div className="banner ok">{info}</div>}
      <label>Email<input name="email" type="email" required /></label>
      <button className="btn" type="submit">Gửi liên kết</button>
    </form>
  );
}

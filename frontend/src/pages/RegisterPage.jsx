import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { api } from '../api';

export function RegisterPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const [error, setError] = useState('');

  async function onSubmit(event) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    setError('');
    try {
      const result = await api('/api/auth/register', { method: 'POST', body: data });
      navigate('/login', { state: { message: result.message, kind: 'ok', from: location.state?.from } });
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <form className="panel stack account-card" onSubmit={onSubmit}>
      <p className="eyebrow">HBS</p>
      <h1>Mời bạn gia nhập</h1>
      <p className="lede">Tạo tài khoản để giữ phòng cho chuyến đi.</p>
      {error && <div className="banner">{error}</div>}
      <label>Họ tên<input name="fullName" required /></label>
      <label>Email<input name="email" type="email" required /></label>
      <label>Số điện thoại<input name="phoneNumber" pattern="0[0-9]{9}" /></label>
      <label>Mật khẩu<input name="password" type="password" required /></label>
      <label>Nhập lại mật khẩu<input name="confirmPassword" type="password" required /></label>
      <button className="btn" type="submit">Tạo tài khoản</button>
    </form>
  );
}

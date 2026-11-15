import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api';
import { money } from '../format';
import { AsyncView, useLoad } from '../ui';

export function PromoPage() {
  const navigate = useNavigate();
  const [tick, setTick] = useState(0);
  const [error, setError] = useState('');
  const state = useLoad(() => api('/api/admin/promo-codes'), [tick]);

  function done(message) {
    setTick((value) => value + 1);
    navigate('/admin/promos', { state: { message, kind: 'ok' } });
  }

  async function create(event) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    const percent = Number(data.discountPercent || 0);
    const amount = Number(data.discountAmount || 0);
    if (percent > 0 && amount > 0) {
      setError('Chỉ nhập phần trăm hoặc số tiền, không nhập cả hai.');
      return;
    }
    setError('');
    try {
      const result = await api('/api/admin/promo-codes', {
        method: 'POST',
        body: {
          code: data.code,
          discountPercent: percent,
          discountAmount: amount,
          isActive: true
        }
      });
      event.target.reset();
      done(result.message);
    } catch (err) {
      setError(err.message);
    }
  }

  async function toggle(id) {
    setError('');
    try {
      const result = await api(`/api/admin/promo-codes/${id}/toggle`, { method: 'POST' });
      done(result.message);
    } catch (err) {
      setError(err.message);
    }
  }

  async function remove(id) {
    setError('');
    try {
      const result = await api(`/api/admin/promo-codes/${id}`, { method: 'DELETE' });
      done(result.message);
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <AsyncView state={state}>
      {(codes) => (
        <>
          <header className="page-head">
            <p className="eyebrow">Quản trị</p>
            <h1>Mã giảm giá</h1>
          </header>
          {error && <div className="banner">{error}</div>}
          <form className="panel form-grid" onSubmit={create}>
            <label>Mã<input name="code" placeholder="WELCOME10" required /></label>
            <label>Giảm %<input name="discountPercent" type="number" min="0" max="100" defaultValue="0" /></label>
            <label>Hoặc giảm tiền<input name="discountAmount" type="number" min="0" defaultValue="0" /></label>
            <button className="btn" type="submit">Thêm mã</button>
          </form>
          <div className="table-wrap">
            <table>
              <thead><tr><th>Mã</th><th>Mức giảm</th><th>Trạng thái</th><th></th></tr></thead>
              <tbody>
                {codes.length === 0 && <tr><td colSpan="4">Chưa có mã.</td></tr>}
                {codes.map((code) => (
                  <tr key={code.promoCodeId}>
                    <td>{code.code}</td>
                    <td>{code.discountPercent > 0 ? `${code.discountPercent}%` : money(code.discountAmount)}</td>
                    <td>{code.isActive ? 'Đang dùng' : 'Tạm dừng'}</td>
                    <td className="row">
                      <button className="btn small secondary" type="button" onClick={() => toggle(code.promoCodeId)}>{code.isActive ? 'Tạm dừng' : 'Bật lại'}</button>
                      <button className="btn small danger" type="button" onClick={() => remove(code.promoCodeId)}>Xóa</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </AsyncView>
  );
}

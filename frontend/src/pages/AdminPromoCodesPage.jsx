import { useState } from 'react';
import { api } from '../api';
import { money } from '../format';
import { AsyncView, useLoad } from '../ui';

export function AdminPromoCodesPage() {
  const [tick, setTick] = useState(0);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const [busy, setBusy] = useState(false);

  const [form, setForm] = useState({
    code: '',
    discountType: 'percent', // 'percent' or 'amount'
    discountValue: 10,
    isActive: true,
  });

  const state = useLoad(() => api('/api/admin/promo-codes'), [tick]);

  function refresh(msg) {
    setError('');
    setNotice(msg || '');
    setShowCreate(false);
    setTick((t) => t + 1);
  }

  async function handleToggle(id) {
    setError('');
    try {
      const res = await api(`/api/admin/promo-codes/${id}/toggle`, { method: 'POST' });
      refresh(res.message);
    } catch (err) {
      setError(err.message);
    }
  }

  async function handleDelete(id, code) {
    if (!window.confirm(`Bạn có chắc chắn muốn xóa mã '${code}'?`)) return;
    setError('');
    try {
      const res = await api(`/api/admin/promo-codes/${id}`, { method: 'DELETE' });
      refresh(res.message);
    } catch (err) {
      setError(err.message);
    }
  }

  async function handleCreate(e) {
    e.preventDefault();
    setBusy(true);
    setError('');
    try {
      const payload = {
        code: form.code.trim().toUpperCase(),
        discountPercent: form.discountType === 'percent' ? Number(form.discountValue) : 0,
        discountAmount: form.discountType === 'amount' ? Number(form.discountValue) : 0,
        isActive: form.isActive,
      };

      const res = await api('/api/admin/promo-codes', {
        method: 'POST',
        body: payload,
      });

      setForm({ code: '', discountType: 'percent', discountValue: 10, isActive: true });
      refresh(res.message);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="stack" style={{ maxWidth: 1000, margin: '0 auto' }}>
      <div className="spread">
        <div>
          <h1>Mã khuyến mãi & Ưu đãi</h1>
          <p className="muted">Quản lý các mã giảm giá áp dụng khi khách đặt phòng hoặc đặt qua điện thoại.</p>
        </div>
        <button className="btn" type="button" onClick={() => setShowCreate((s) => !s)}>
          {showCreate ? '✕ Đóng form' : '✚ Tạo mã mới'}
        </button>
      </div>

      {notice && <div className="banner ok">{notice}</div>}
      {error && <div className="banner">{error}</div>}

      {showCreate && (
        <form className="panel stack" onSubmit={handleCreate}>
          <h2>Tạo mã khuyến mãi mới</h2>
          <div className="form-grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem' }}>
            <label>
              Mã Code *
              <input
                value={form.code}
                onChange={(e) => setForm({ ...form, code: e.target.value.toUpperCase() })}
                placeholder="VD: HE2026, GIAM50K"
                required
                minLength={3}
                maxLength={20}
              />
            </label>
            <label>
              Hình thức giảm
              <select
                value={form.discountType}
                onChange={(e) => setForm({ ...form, discountType: e.target.value, discountValue: e.target.value === 'percent' ? 10 : 100000 })}
              >
                <option value="percent">Giảm theo % (Phần trăm)</option>
                <option value="amount">Giảm tiền mặt cố định (VNĐ)</option>
              </select>
            </label>
            <label>
              {form.discountType === 'percent' ? 'Mức giảm (%) *' : 'Số tiền giảm (VNĐ) *'}
              <input
                type="number"
                min="1"
                max={form.discountType === 'percent' ? 100 : 100000000}
                step={form.discountType === 'percent' ? 1 : 10000}
                value={form.discountValue}
                onChange={(e) => setForm({ ...form, discountValue: Number(e.target.value) })}
                required
              />
            </label>
            <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginTop: '1.5rem' }}>
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
              />
              Kích hoạt áp dụng ngay
            </label>
          </div>
          <div className="row" style={{ marginTop: '0.5rem' }}>
            <button className="btn" type="submit" disabled={busy}>
              {busy ? 'Đang tạo...' : 'Xác nhận tạo mã'}
            </button>
            <button className="btn secondary" type="button" onClick={() => setShowCreate(false)}>
              Hủy
            </button>
          </div>
        </form>
      )}

      <AsyncView state={state}>
        {(promos) => (
          <div className="table-wrap panel">
            <table>
              <thead>
                <tr>
                  <th>Mã Code</th>
                  <th>Mức giảm giá</th>
                  <th>Trạng thái</th>
                  <th style={{ textAlign: 'right' }}>Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {(!promos || promos.length === 0) && (
                  <tr>
                    <td colSpan="4" style={{ textAlign: 'center', padding: '2rem' }}>
                      Chưa có mã khuyến mãi nào. Hãy tạo mã đầu tiên!
                    </td>
                  </tr>
                )}
                {(promos || []).map((p) => {
                  const discountLabel = p.discountPercent > 0
                    ? `Giảm ${p.discountPercent}%`
                    : `Giảm ${money(p.discountAmount)}`;

                  return (
                    <tr key={p.promoCodeId}>
                      <td>
                        <strong style={{ fontFamily: 'monospace', fontSize: '1.1rem', letterSpacing: '0.05em', color: '#1f4d45' }}>
                          {p.code}
                        </strong>
                      </td>
                      <td>
                        <span className="pill" style={{ background: '#f0f7f4', color: '#1f4d45', fontWeight: 600 }}>
                          {discountLabel}
                        </span>
                      </td>
                      <td>
                        {p.isActive ? (
                          <span className="pill ok">Đang hoạt động</span>
                        ) : (
                          <span className="pill" style={{ background: '#e0e0e0', color: '#666' }}>Tạm dừng</span>
                        )}
                      </td>
                      <td style={{ textAlign: 'right' }}>
                        <div className="row" style={{ justifyContent: 'flex-end', gap: '0.5rem' }}>
                          <button
                            className="btn small secondary"
                            type="button"
                            onClick={() => handleToggle(p.promoCodeId)}
                          >
                            {p.isActive ? 'Tạm dừng' : 'Kích hoạt'}
                          </button>
                          <button
                            className="btn small"
                            type="button"
                            style={{ background: '#9d3b32', borderColor: '#9d3b32' }}
                            onClick={() => handleDelete(p.promoCodeId, p.code)}
                          >
                            Xóa
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </AsyncView>
    </div>
  );
}

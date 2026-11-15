import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api';
import { money } from '../format';
import { AsyncView, useLoad } from '../ui';

export function RoomTypesPage() {
  const navigate = useNavigate();
  const [tick, setTick] = useState(0);
  const [error, setError] = useState('');
  const state = useLoad(() => api('/api/admin/room-types'), [tick]);

  async function onSubmit(event) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    setError('');
    try {
      const result = await api('/api/admin/room-types', {
        method: 'POST',
        body: {
          typeName: data.typeName,
          description: data.description,
          basePrice: Number(data.basePrice),
          maxGuests: Number(data.maxGuests),
          isActive: true
        }
      });
      event.target.reset();
      setTick((value) => value + 1);
      navigate('/admin/types', { state: { message: result.message, kind: 'ok' } });
    } catch (err) {
      setError(err.message);
    }
  }

  async function remove(id) {
    setError('');
    try {
      const result = await api(`/api/admin/room-types/${id}`, { method: 'DELETE' });
      setTick((value) => value + 1);
      navigate('/admin/types', { state: { message: result.message, kind: 'ok' } });
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <AsyncView state={state}>
      {(types) => (
        <>
          <h1>Loại phòng</h1>
          {error && <div className="banner">{error}</div>}
          <form className="panel form-grid" onSubmit={onSubmit}>
            <label>Tên<input name="typeName" required /></label>
            <label>Giá<input name="basePrice" type="number" min="0" required /></label>
            <label>Số khách<input name="maxGuests" type="number" min="1" defaultValue="2" required /></label>
            <label>Mô tả<input name="description" /></label>
            <button className="btn" type="submit">Thêm</button>
          </form>
          <div className="table-wrap">
            <table>
              <thead><tr><th>Tên</th><th>Giá</th><th>Khách</th><th>Đang dùng</th><th></th></tr></thead>
              <tbody>
                {types.map((type) => (
                  <tr key={type.roomTypeId}>
                    <td>{type.typeName}<div className="muted">{type.description}</div></td>
                    <td>{money(type.basePrice)}</td>
                    <td>{type.maxGuests}</td>
                    <td>{type.isActive ? 'Có' : 'Không'}</td>
                    <td><button className="btn small danger" type="button" onClick={() => remove(type.roomTypeId)}>Xóa</button></td>
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

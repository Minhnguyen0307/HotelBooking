import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api } from '../api';
import { AsyncView, useLoad } from '../ui';

const ROOM_STATUSES = ['Available', 'Booked', 'Maintenance'];

export function AdminRoomsPage() {
  const navigate = useNavigate();
  const [tick, setTick] = useState(0);
  const [error, setError] = useState('');
  const state = useLoad(() => api('/api/admin/rooms'), [tick]);

  async function remove(id) {
    setError('');
    try {
      const result = await api(`/api/admin/rooms/${id}`, { method: 'DELETE' });
      setTick((value) => value + 1);
      navigate('/admin/rooms', { state: { message: result.message, kind: 'ok' } });
    } catch (err) {
      setError(err.message);
    }
  }

  async function setStatus(id, status) {
    setError('');
    try {
      const result = await api(`/api/admin/rooms/${id}/status`, { method: 'POST', body: { status } });
      navigate('/admin/rooms', { state: { message: result.message, kind: 'ok' } });
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <AsyncView state={state}>
      {(rooms) => (
        <>
          <header className="page-head">
            <p className="eyebrow">Quản trị</p>
            <div className="spread">
              <h1>Phòng</h1>
              <div className="row">
                <Link to="/admin/types">Loại phòng</Link>
                <Link className="btn" to="/admin/rooms/new">Thêm phòng</Link>
              </div>
            </div>
          </header>
          {error && <div className="banner">{error}</div>}
          <div className="table-wrap">
            <table>
              <thead>
                <tr><th>Cơ sở</th><th>Số</th><th>Loại</th><th>Tầng</th><th>Trạng thái</th><th></th></tr>
              </thead>
              <tbody>
                {rooms.map((room) => (
                  <tr key={room.roomId}>
                    <td>{room.propertyName}</td>
                    <td>{room.roomNumber}</td>
                    <td>{room.typeName}</td>
                    <td>{room.floor ?? ''}</td>
                    <td>
                      <select defaultValue={room.status} onChange={(event) => setStatus(room.roomId, event.target.value)}>
                        {ROOM_STATUSES.map((status) => <option key={status}>{status}</option>)}
                      </select>
                    </td>
                    <td className="row">
                      <Link to={`/admin/rooms/${room.roomId}`}>Sửa</Link>
                      <button className="btn small danger" type="button" onClick={() => remove(room.roomId)}>Xóa</button>
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

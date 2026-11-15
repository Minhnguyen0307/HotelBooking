import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { api } from '../api';
import { AsyncView, useLoad } from '../ui';

const ROOM_STATUSES = ['Available', 'Booked', 'Maintenance'];

export function RoomFormPage() {
  const { id } = useParams();
  const isNew = !id;
  const navigate = useNavigate();
  const [error, setError] = useState('');
  const state = useLoad(async () => {
    const types = await api('/api/admin/rooms/types');
    const properties = await api('/api/rooms/properties');
    const room = isNew ? { status: 'Available', propertyId: properties[0]?.propertyId } : await api(`/api/admin/rooms/${id}`);
    return { types, properties, room };
  }, [id]);

  async function onSubmit(event) {
    event.preventDefault();
    setError('');
    try {
      const result = await api(isNew ? '/api/admin/rooms' : `/api/admin/rooms/${id}`, {
        method: isNew ? 'POST' : 'PUT',
        body: new FormData(event.target)
      });
      navigate('/admin/rooms', { state: { message: result.message, kind: 'ok' } });
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <AsyncView state={state}>
      {({ types, properties, room }) => (
        <form className="panel stack" onSubmit={onSubmit}>
          <h1>{isNew ? 'Thêm phòng' : `Sửa phòng ${room.roomNumber}`}</h1>
          {error && <div className="banner">{error}</div>}
          <label>Số phòng<input name="roomNumber" defaultValue={room.roomNumber || ''} required /></label>
          <label>
            Loại phòng
            <select name="roomTypeId" defaultValue={room.roomTypeId || types[0]?.roomTypeId} required>
              {types.map((type) => <option value={type.roomTypeId} key={type.roomTypeId}>{type.typeName}</option>)}
            </select>
          </label>
          <label>Tầng<input name="floor" type="number" defaultValue={room.floor ?? ''} /></label>
          <label>
            Trạng thái
            <select name="status" defaultValue={room.status || 'Available'}>
              {ROOM_STATUSES.map((status) => <option key={status}>{status}</option>)}
            </select>
          </label>
          <label>
            Cơ sở
            <select name="propertyId" defaultValue={room.propertyId || properties[0]?.propertyId} required>
              {properties.map((property) => (
                <option value={property.propertyId} key={property.propertyId}>{property.name} · {property.area}</option>
              ))}
            </select>
          </label>
          <label>Mô tả<textarea name="description" defaultValue={room.description || ''}></textarea></label>
          <label>Ảnh<input name="imageFile" type="file" accept="image/*" /></label>
          <button className="btn" type="submit">Lưu</button>
        </form>
      )}
    </AsyncView>
  );
}

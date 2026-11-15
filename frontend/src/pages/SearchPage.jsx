import { useNavigate, useSearchParams } from 'react-router-dom';
import { api } from '../api';
import { searchParams, today, tomorrow } from '../format';
import { AsyncView, RoomGrid, StayDates, TypeSelect, useLoad } from '../ui';

export function SearchPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const criteria = {
    checkInDate: params.get('checkInDate') || today(),
    checkOutDate: params.get('checkOutDate') || tomorrow(),
    guests: params.get('guests') || '',
    roomTypeId: params.get('roomTypeId') || ''
  };
  const state = useLoad(async () => {
    sessionStorage.setItem('search', JSON.stringify(criteria));
    const types = await api('/api/rooms/types');
    const rooms = await api(`/api/rooms/search?${searchParams(criteria)}`);
    return { types, rooms };
  }, [criteria.checkInDate, criteria.checkOutDate, criteria.guests, criteria.roomTypeId]);

  function onSearch(event) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    navigate(`/search?${searchParams(data)}`);
  }

  return (
    <AsyncView state={state}>
      {({ types, rooms }) => (
        <>
          <h1>Tìm phòng</h1>
          <form className="panel form-grid" onSubmit={onSearch}>
            <StayDates checkIn={criteria.checkInDate} checkOut={criteria.checkOutDate} />
            <label>Số khách<input name="guests" type="number" min="1" defaultValue={criteria.guests} /></label>
            <TypeSelect types={types} value={criteria.roomTypeId} />
            <button className="btn" type="submit">Tìm</button>
          </form>
          <p className="muted">{rooms.length} phòng trống</p>
          <RoomGrid rooms={rooms} />
        </>
      )}
    </AsyncView>
  );
}

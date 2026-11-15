import { useNavigate, useSearchParams } from 'react-router-dom';
import { api } from '../api';
import { searchParams, today, tomorrow } from '../format';
import { AsyncView, PropertySelect, RoomGrid, StayDates, TypeSelect, useLoad } from '../ui';

export function SearchPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const criteria = {
    checkInDate: params.get('checkInDate') || today(),
    checkOutDate: params.get('checkOutDate') || tomorrow(),
    guests: params.get('guests') || '',
    roomTypeId: params.get('roomTypeId') || '',
    propertyId: params.get('propertyId') || ''
  };
  const state = useLoad(async () => {
    sessionStorage.setItem('search', JSON.stringify(criteria));
    const types = await api('/api/rooms/types');
    const properties = await api('/api/rooms/properties');
    const rooms = criteria.propertyId
      ? await api(`/api/rooms/catalog?${searchParams(criteria)}`)
      : await api(`/api/rooms/search?${searchParams(criteria)}`);
    return { types, properties, rooms };
  }, [criteria.checkInDate, criteria.checkOutDate, criteria.guests, criteria.roomTypeId, criteria.propertyId]);

  function onSearch(event) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    navigate(`/search?${searchParams(data)}`);
  }

  return (
    <AsyncView state={state}>
      {({ types, properties, rooms }) => (
        <>
          <header className="page-intro">
            <p className="eyebrow">Đặt phòng</p>
            <h1>Mời bạn tìm kỳ nghỉ</h1>
          </header>
          <form className="panel form-grid" onSubmit={onSearch}>
            <StayDates checkIn={criteria.checkInDate} checkOut={criteria.checkOutDate} />
            <PropertySelect properties={properties} value={criteria.propertyId} />
            <label>Số khách<input name="guests" type="number" min="1" defaultValue={criteria.guests} /></label>
            <TypeSelect types={types} value={criteria.roomTypeId} />
            <button className="btn" type="submit">Tìm</button>
          </form>
          <p className="muted stay-count">
            {criteria.propertyId
              ? `${rooms.filter((room) => room.isAvailable !== false).length} phòng sẵn sàng${rooms.some((room) => room.isAvailable === false) ? ` · ${rooms.filter((room) => room.isAvailable === false).length} phòng đã kín` : ''}`
              : `${rooms.length} phòng phù hợp`}
          </p>
          <RoomGrid rooms={rooms} showAvailability={Boolean(criteria.propertyId)} />
        </>
      )}
    </AsyncView>
  );
}

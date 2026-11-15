import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { api } from '../api';
import { useAuth } from '../auth';
import { money, searchParams, today, tomorrow } from '../format';
import { LuxBar } from '../lux';
import { AsyncView, PropertySelect, StayDates, TypeSelect, useLoad } from '../ui';

export function SearchPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const criteria = {
    checkInDate: params.get('checkInDate') || today(),
    checkOutDate: params.get('checkOutDate') || tomorrow(),
    guests: params.get('guests') || '2',
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
    <div className="lux lux-page">
      <LuxBar solid />
      <AsyncView state={state}>
        {({ types, properties, rooms }) => {
          const branch = properties.find((item) => String(item.propertyId) === String(criteria.propertyId));
          const open = rooms.filter((room) => room.isAvailable !== false);
          return (
            <section className="lux-book-page">
              <p className="lux-kicker dark">HBS Reservation</p>
              <h1>Kết quả phòng trống</h1>
              <form className="lux-search sheet" onSubmit={onSearch}>
                <PropertySelect properties={properties} value={criteria.propertyId} />
                <StayDates checkIn={criteria.checkInDate} checkOut={criteria.checkOutDate} />
                <label>Số khách<input name="guests" type="number" min="1" defaultValue={criteria.guests} /></label>
                <TypeSelect types={types} value={criteria.roomTypeId} />
                <button type="submit">Tìm phòng</button>
              </form>
              <div className="lux-stay">
                <strong>{branch ? branch.name : 'Tất cả cơ sở Đà Nẵng'}</strong>
                <span>Thời gian: {showDate(criteria.checkInDate)} đến {showDate(criteria.checkOutDate)}</span>
                <span>Khách lưu trú: {criteria.guests} khách</span>
                <span>{open.length} phòng còn trống</span>
              </div>
              <div className="lux-picks">
                {rooms.map((room) => {
                  const sold = room.isAvailable === false;
                  return (
                    <article className={`lux-pick${sold ? ' is-sold' : ''}`} key={room.roomId}>
                      <div>
                        <h2>Phòng {room.roomNumber}</h2>
                        <p>{room.propertyName} · {room.stayLabel || room.typeName} · gồm {room.includedGuests || room.maxGuests} khách</p>
                        <span>{sold ? 'Hết phòng trong khoảng ngày này' : 'Còn phòng · giá mỗi đêm'}</span>
                      </div>
                      <div>
                        <strong>{money(room.basePrice)}</strong>
                        {sold
                          ? <span className="lux-wait">Hết phòng</span>
                          : user
                            ? <Link to={`/rooms/${room.roomId}`}>Chọn</Link>
                            : <button type="button" onClick={() => navigate('/login', { state: { from: `/rooms/${room.roomId}` } })}>Chọn</button>}
                      </div>
                    </article>
                  );
                })}
              </div>
              {rooms.length === 0 && <p className="lux-note">Chưa có phòng trống cho ngày bạn chọn.</p>}
            </section>
          );
        }}
      </AsyncView>
    </div>
  );
}

function showDate(iso) {
  const date = new Date(`${iso}T00:00:00`);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleDateString('vi-VN');
}

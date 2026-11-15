import { useNavigate } from 'react-router-dom';
import { api } from '../api';
import { searchParams, today, tomorrow } from '../format';
import { AsyncView, RoomGrid, StayDates, TypeSelect, useLoad } from '../ui';

export function HomePage() {
  const navigate = useNavigate();
  const state = useLoad(() => Promise.all([
    api('/api/health'),
    api('/api/rooms/featured'),
    api('/api/rooms/types')
  ]), []);

  function onSearch(event) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    navigate(`/search?${searchParams(data)}`);
  }

  return (
    <AsyncView state={state}>
      {([health, rooms, types]) => (
        <>
          <section className="hero">
            <div>
              <p className="pill">Database: {health.database} · {health.rooms} phòng · {health.users} tài khoản</p>
              <h1>Nghỉ ở Đà Nẵng, đặt phòng trong vài phút.</h1>
              <p className="lede">Tìm phòng trống theo ngày, giữ chỗ và thanh toán ngay trên hệ thống.</p>
            </div>
            <form className="panel stack" onSubmit={onSearch}>
              <StayDates checkIn={today()} checkOut={tomorrow()} />
              <label>Số khách<input name="guests" type="number" min="1" defaultValue="2" /></label>
              <TypeSelect types={types} />
              <button className="btn" type="submit">Tìm phòng trống</button>
            </form>
          </section>
          <h2>Phòng trống hôm nay</h2>
          <RoomGrid rooms={rooms} />
        </>
      )}
    </AsyncView>
  );
}

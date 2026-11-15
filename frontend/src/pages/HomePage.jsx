import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api } from '../api';
import { addDays, money, searchParams, today, tomorrow } from '../format';
import { LuxBar } from '../lux';
import { AsyncView, useLoad } from '../ui';

const HERO = 'https://images.unsplash.com/photo-1542314831-068cd1dbfeeb?auto=format&fit=crop&w=2000&q=80';
const STORY = 'https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&w=1200&q=80';
const STORY_DETAIL = 'https://images.unsplash.com/photo-1582719508461-905c673771fd?auto=format&fit=crop&w=900&q=80';
const DINING = 'https://images.unsplash.com/photo-1514362545857-3bc16c4c7d1b?auto=format&fit=crop&w=1200&q=80';
const SPA = 'https://images.unsplash.com/photo-1544161515-4ab6ce6db874?auto=format&fit=crop&w=900&q=80';
const POOL = 'https://images.unsplash.com/photo-1576013551627-0cc20b96c2a7?auto=format&fit=crop&w=900&q=80';
const STANDARD = 'https://images.unsplash.com/photo-1618773928121-c32242e63f39?auto=format&fit=crop&w=1000&q=80';
const DELUXE = 'https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=1000&q=80';
const SUITE = 'https://images.unsplash.com/photo-1613490493576-7fde63acd811?auto=format&fit=crop&w=1000&q=80';

const FILTERS = [
  ['all', 'Tất cả'],
  ['standard', 'Phòng đơn'],
  ['deluxe', 'Phòng cặp đôi'],
  ['suite', 'Phòng gia đình']
];

export function HomePage() {
  const navigate = useNavigate();
  const [selectedId, setSelectedId] = useState('');
  const [filter, setFilter] = useState('all');
  const [guests, setGuests] = useState(2);
  const [dates, setDates] = useState({ checkInDate: today(), checkOutDate: tomorrow() });
  const state = useLoad(() => Promise.all([
    api('/api/health'),
    api('/api/rooms/featured'),
    api('/api/rooms/properties')
  ]), []);
  const catalog = useLoad(async () => {
    if (!selectedId) return null;
    return api(`/api/rooms/catalog?${searchParams({ ...dates, propertyId: selectedId })}`);
  }, [selectedId, dates.checkInDate, dates.checkOutDate]);

  useEffect(() => {
    sessionStorage.setItem('search', JSON.stringify({ ...dates, guests, propertyId: selectedId }));
  }, [dates, guests, selectedId]);

  function chooseProperty(propertyId) {
    setSelectedId(String(propertyId));
    setFilter('all');
    document.getElementById('rooms')?.scrollIntoView({ behavior: 'smooth' });
  }

  function onSearch(event) {
    event.preventDefault();
    navigate(`/search?${searchParams({ ...dates, guests, propertyId: selectedId })}`);
  }

  return (
    <div className="lux">
      <LuxBar home />
      <AsyncView state={state}>
      {([health, featured, properties]) => {
        const source = selectedId
          ? (Array.isArray(catalog.data) ? catalog.data : [])
          : featured;
        const rooms = (source || []).filter((room) => matchesFilter(room, filter));
        return (
          <>
            <section className="lux-hero" id="hero">
              <img src={HERO} alt="Sảnh nghỉ dưỡng HBS bên biển Đà Nẵng" />
              <div className="lux-hero-copy">
                <p className="lux-kicker">Nghỉ dưỡng bên biển Đà Nẵng</p>
                <h1>Nơi tinh hoa kiến trúc <span>giao hòa cùng vịnh</span></h1>
                <p>Năm cơ sở HBS trải từ Mỹ Khê, Sơn Trà, Hải Châu, Ngũ Hành Sơn đến Liên Chiểu. {health.rooms} phòng đang mở cho kỳ nghỉ của bạn.</p>
              </div>
              <form className="lux-search" id="booking" onSubmit={onSearch}>
                <label>Điểm đến
                  <select value={selectedId} onChange={(event) => setSelectedId(event.target.value)}>
                    <option value="">Tất cả cơ sở</option>
                    {properties.map((property) => (
                      <option value={property.propertyId} key={property.propertyId}>{property.name}</option>
                    ))}
                  </select>
                </label>
                <label>Nhận phòng
                  <input type="date" value={dates.checkInDate} min={today()} onChange={(event) => setDates((current) => ({
                    checkInDate: event.target.value,
                    checkOutDate: current.checkOutDate > event.target.value ? current.checkOutDate : addDays(event.target.value, 1)
                  }))} required />
                </label>
                <label>Trả phòng
                  <input type="date" value={dates.checkOutDate} min={addDays(dates.checkInDate, 1)} onChange={(event) => setDates((current) => ({ ...current, checkOutDate: event.target.value }))} required />
                </label>
                <label>Số khách
                  <input type="number" min="1" max="6" value={guests} onChange={(event) => setGuests(Number(event.target.value))} />
                </label>
                <button type="submit">Tìm phòng</button>
              </form>
            </section>

            <section className="lux-story" id="branches">
              <div className="lux-story-photos">
                <img src={STORY} alt="Sảnh HBS" />
                <img src={STORY_DETAIL} alt="Nội thất phòng HBS" />
              </div>
              <div>
                <p className="lux-kicker dark">Câu chuyện thương hiệu</p>
                <h2>Nghệ thuật hiếu khách <span>dọc bờ biển Đà Nẵng</span></h2>
                <p>Mỗi cơ sở HBS giữ một nhịp riêng: cát Mỹ Khê, bán đảo Sơn Trà, sông Hàn, Ngũ Hành Sơn và cửa ngõ sân bay Liên Chiểu.</p>
                <div className="lux-stats">
                  <div><strong>{properties.length}</strong><span>Cơ sở tại Đà Nẵng</span></div>
                  <div><strong>{health.rooms}</strong><span>Phòng đang phục vụ</span></div>
                  <div><strong>14:00</strong><span>Giờ nhận phòng</span></div>
                </div>
                <div className="lux-pills">
                  {properties.map((property) => (
                    <button
                      type="button"
                      key={property.propertyId}
                      className={String(property.propertyId) === String(selectedId) ? 'on' : ''}
                      onClick={() => chooseProperty(property.propertyId)}
                    >
                      {property.area}
                    </button>
                  ))}
                </div>
              </div>
            </section>

            <section className="lux-rooms" id="rooms">
              <div className="lux-rooms-head">
                <div>
                  <p className="lux-kicker dark">Không gian lưu trú</p>
                  <h2>Hạng phòng <span>của HBS</span></h2>
                </div>
                <div className="lux-filters">
                  {FILTERS.map(([id, label]) => (
                    <button key={id} type="button" className={filter === id ? 'on' : ''} onClick={() => setFilter(id)}>{label}</button>
                  ))}
                </div>
              </div>
              {selectedId && (catalog.loading || !Array.isArray(catalog.data)) && <p className="lux-note">Đang tải phòng của cơ sở...</p>}
              {catalog.error && <p className="lux-note">{catalog.error.message}</p>}
              <div className="lux-room-grid">
                {rooms.map((room) => {
                  const sold = room.isAvailable === false;
                  return (
                    <Link className={`lux-room${sold ? ' is-sold' : ''}`} to={`/rooms/${room.roomId}`} key={room.roomId}>
                      <div className="lux-room-photo">
                        <img src={roomPhoto(room)} alt={`Phòng ${room.roomNumber}`} />
                        <span>{sold ? 'Hết phòng' : room.stayLabel || room.typeName}</span>
                      </div>
                      <div className="lux-room-body">
                        <p>{room.propertyName} · gồm {room.includedGuests || room.maxGuests} khách</p>
                        <h3>Phòng {room.roomNumber}</h3>
                        <div>
                          <small>Giá mỗi đêm từ</small>
                          <strong>{money(room.basePrice)}</strong>
                        </div>
                      </div>
                    </Link>
                  );
                })}
              </div>
              {rooms.length === 0 && <p className="lux-note">Chưa có phòng phù hợp bộ lọc này.</p>}
            </section>

            <section className="lux-exp" id="experiences">
              <div className="lux-exp-intro">
                <p className="lux-kicker">Đánh thức mọi giác quan</p>
                <h2>Trải nghiệm <span>bên trong kỳ nghỉ</span></h2>
              </div>
              <div className="lux-bento">
                <article className="wide">
                  <img src={DINING} alt="Nhà hàng HBS" />
                  <div><span>Ẩm thực</span><h3>Nhà hàng bên sông và biển</h3><p>Hải sản Đà Nẵng, bữa sáng và bữa tối ghi nhận vào hóa đơn phòng.</p></div>
                </article>
                <article>
                  <img src={SPA} alt="Spa HBS" />
                  <div><span>Spa</span><h3>Giờ thư giãn</h3><p>Liệu trình spa được lễ tân cộng vào bill khi trả phòng.</p></div>
                </article>
                <article>
                  <img src={POOL} alt="Hồ bơi HBS" />
                  <div><span>Hồ bơi</span><h3>Nước và hoàng hôn</h3><p>Hồ bơi ngoài trời tại các cơ sở sát biển Mỹ Khê và Sơn Trà.</p></div>
                </article>
                <article className="offer" id="offers">
                  <p className="lux-kicker">Ưu đãi đặt trực tiếp</p>
                  <h3>Giảm 10% tiền phòng với mã WELCOME10</h3>
                  <p>Nhập mã khi đặt trên web hoặc khi gọi điện cho lễ tân. Mã GIAM100 giảm thẳng 100.000 ₫.</p>
                  <a href="#booking">Dùng ưu đãi khi tìm phòng</a>
                </article>
              </div>
            </section>

            <footer className="lux-foot">
              <div>
                <strong>HBS</strong>
                <p>Chuỗi khách sạn Đà Nẵng. Nhận phòng 14:00, trả phòng 12:00.</p>
              </div>
              <div>
                <h3>Khám phá</h3>
                <a href="#rooms">Hạng phòng</a>
                <Link to="/search">Tìm phòng trống</Link>
                <Link to="/privacy">Bảo mật</Link>
              </div>
              <div>
                <h3>Chi nhánh</h3>
                {properties.map((property) => <span key={property.propertyId}>{property.name} · {property.address}</span>)}
              </div>
            </footer>
          </>
        );
      }}
      </AsyncView>
    </div>
  );
}

function matchesFilter(room, filter) {
  const name = (room.typeName || '').toLowerCase();
  if (filter === 'suite') return name.includes('suite');
  if (filter === 'deluxe') return name.includes('deluxe');
  if (filter === 'standard') return name.includes('standard');
  return true;
}

function roomPhoto(room) {
  if (room.primaryImageUrl) return room.primaryImageUrl;
  const name = (room.typeName || '').toLowerCase();
  if (name.includes('suite')) return SUITE;
  if (name.includes('deluxe')) return DELUXE;
  return STANDARD;
}

import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api';
import { searchParams, today, tomorrow } from '../format';
import { AsyncView, PropertySelect, RoomGrid, StayDates, TypeSelect, useLoad } from '../ui';

export function HomePage() {
  const navigate = useNavigate();
  const [selectedId, setSelectedId] = useState(null);
  const [dates, setDates] = useState({ checkInDate: today(), checkOutDate: tomorrow() });
  const state = useLoad(async () => {
    const results = await Promise.all([
      api('/api/rooms/featured'),
      api('/api/rooms/types'),
      api('/api/rooms/properties')
    ].map((request) => request.then((data) => ({ data })).catch((error) => ({ error }))));
    if (results.every((result) => result.error)) throw results[0].error;
    return {
      rooms: results[0].data || [],
      types: results[1].data || [],
      properties: results[2].data || [],
      loadErrors: results.filter((result) => result.error).map((result) => result.error)
    };
  }, []);
  const catalog = useLoad(async () => {
    if (!selectedId) return null;
    return api(`/api/rooms/catalog?${searchParams({ ...dates, propertyId: selectedId })}`);
  }, [selectedId, dates.checkInDate, dates.checkOutDate]);

  useEffect(() => {
    sessionStorage.setItem('search', JSON.stringify({ ...dates, propertyId: selectedId || '' }));
  }, [dates, selectedId]);

  function onSearch(event) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    navigate(`/search?${searchParams(data)}`);
  }

  function onDates(next) {
    setDates((current) => (
      current.checkInDate === next.checkInDate && current.checkOutDate === next.checkOutDate ? current : next
    ));
  }

  function chooseProperty(propertyId) {
    setSelectedId((current) => (current === propertyId ? null : propertyId));
  }

  return (
    <AsyncView state={state}>
      {({ rooms, types, properties, loadErrors }) => {
        const selected = properties.find((property) => property.propertyId === selectedId) || null;
        const roomCount = properties.reduce((total, property) => total + (property.roomCount || 0), 0);
        const listed = Array.isArray(catalog.data)
          && catalog.data.every((room) => room.propertyId === selectedId)
          ? catalog.data
          : null;
        const openCount = listed ? listed.filter((room) => room.isAvailable !== false).length : 0;
        const soldCount = listed ? listed.length - openCount : 0;
        return (
          <>
            <section className="hero">
              <div className="hero-copy">
                <p className="eyebrow">HBS · Đà Nẵng</p>
                <h1>Chào mừng bạn đến với kỳ nghỉ bên biển.</h1>
                <p className="lede">Mời bạn ghé {properties.length} cơ sở của chúng tôi, với {roomCount} phòng riêng giữa Mỹ Khê, sông Hàn và bán đảo Sơn Trà.</p>
              </div>
              <form className="panel stack" onSubmit={onSearch}>
                <StayDates checkIn={dates.checkInDate} checkOut={dates.checkOutDate} onChange={onDates} />
                <PropertySelect properties={properties} />
                <label>Số khách<input name="guests" type="number" min="1" defaultValue="2" /></label>
                <TypeSelect types={types} />
                <button className="btn" type="submit">Tìm phòng trống</button>
              </form>
            </section>

            {loadErrors.length > 0 && (
              <div className="banner" role="status">
                Một số danh sách chưa tải được. Bạn vẫn có thể xem phần dữ liệu đã tải.
                <button className="btn secondary small" type="button" onClick={state.retry}>Tải lại</button>
              </div>
            )}

            <div className="section-head">
              <div>
                <h2>Mời bạn chọn cơ sở</h2>
              </div>
              {selected && (
                <button className="btn secondary" type="button" onClick={() => setSelectedId(null)}>
                  Phòng tiêu biểu
                </button>
              )}
            </div>
            <div className="property-board">
              {properties.map((property, index) => (
                <button
                  className={`property-card${selectedId === property.propertyId ? ' selected' : ''}`}
                  type="button"
                  key={property.propertyId}
                  onClick={() => chooseProperty(property.propertyId)}
                >
                  <span className={`property-mark tone-${index % 5}`}>{property.area.slice(0, 1)}</span>
                  <strong>{property.name}</strong>
                  <p>{property.area}</p>
                  <p className="muted clamp">{property.description}</p>
                  <span className="pill">{property.roomCount} phòng riêng</span>
                </button>
              ))}
            </div>

            {selected ? (
              <section className="block">
                <div className="section-head">
                  <div>
                    <h2>Chào mừng đến {selected.name}</h2>
                    <p className="muted">{selected.address}</p>
                  </div>
                </div>
                <form className="panel form-grid date-bar" onSubmit={(event) => event.preventDefault()}>
                  <StayDates checkIn={dates.checkInDate} checkOut={dates.checkOutDate} onChange={onDates} />
                </form>
                {catalog.error ? (
                  <div className="banner">{catalog.error.message}</div>
                ) : catalog.loading || !listed ? (
                  <p className="muted">Đang tải phòng...</p>
                ) : (
                  <>
                    <p className="muted stay-count">
                      {openCount} phòng sẵn sàng{soldCount > 0 ? ` · ${soldCount} phòng đã kín` : ''}
                    </p>
                    <RoomGrid rooms={listed} showAvailability />
                  </>
                )}
              </section>
            ) : (
              <section className="block">
                <div className="section-head">
                  <div>
                    <h2>Phòng dành cho bạn</h2>
                    <p className="muted">Mời bạn xem những phòng được yêu thích nhất.</p>
                  </div>
                </div>
                <RoomGrid rooms={rooms} featured showAvailability />
              </section>
            )}
          </>
        );
      }}
    </AsyncView>
  );
}

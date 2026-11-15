import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api } from '../api';
import { useAuth } from '../auth';
import { money, today, tomorrow } from '../format';
import { LuxBar } from '../lux';
import { AsyncView, Photo, StarPicker, StarRow, StayDates, useLoad } from '../ui';

export function RoomPage() {
  const { id } = useParams();
  const { user, ready, hasRole } = useAuth();
  const navigate = useNavigate();
  const [tick, setTick] = useState(0);
  const [preview, setPreview] = useState(null);
  const [error, setError] = useState('');
  const [addons, setAddons] = useState({ spa: false, breakfast: false, extraBed: false });
  const saved = JSON.parse(sessionStorage.getItem('search') || '{}');
  const state = useLoad(async () => {
    const [detail, serviceResult, addOnResult, promoResult] = await Promise.all([
      api(`/api/rooms/${id}`),
      api('/api/services').then((data) => ({ data })).catch((error) => ({ error })),
      api('/api/rooms/add-ons').then((data) => ({ data })).catch((error) => ({ error })),
      user ? api('/api/bookings/promos').then((data) => ({ data })).catch((error) => ({ error })) : Promise.resolve({ data: [] })
    ]);
    return {
      ...detail,
      services: serviceResult.data || [],
      servicesLoadError: serviceResult.error,
      addOns: addOnResult.data || [],
      addOnsLoadError: addOnResult.error,
      promoCodes: promoResult.data || [],
      promoLoadError: promoResult.error
    };
  }, [id, tick, user]);
  const catalog = { data: state.data?.services || [] };
  const offers = catalog.data || [];
  const offer = (code) => offers.find((item) => item.serviceCode === code);

  function askLogin() {
    navigate('/login', { state: { from: `/rooms/${id}` } });
  }

  async function onPreview(event) {
    if (!user) {
      askLogin();
      return;
    }
    const form = event.target.form;
    const data = Object.fromEntries(new FormData(form));
    setError('');
    try {
      const result = await api('/api/bookings/preview', {
        method: 'POST',
        body: {
          roomId: Number(id),
          checkInDate: data.checkInDate,
          checkOutDate: data.checkOutDate,
          numberOfGuests: Number(data.numberOfGuests),
          promoCode: (data.promoCode || '').trim(),
          breakfast: addons.breakfast,
          spa: addons.spa,
          extraBed: addons.extraBed,
          addOns: addons.airportTransfer ? [{ code: 'airport-transfer', quantity: 1 }] : []
        }
      });
      setPreview(result);
    } catch (err) {
      setError(err.message);
    }
  }

  async function onBook(event) {
    event.preventDefault();
    if (!user) {
      askLogin();
      return;
    }
    const data = Object.fromEntries(new FormData(event.target));
    setError('');
    try {
      const result = await api('/api/bookings', {
        method: 'POST',
        body: {
          roomId: Number(id),
          checkInDate: data.checkInDate,
          checkOutDate: data.checkOutDate,
          numberOfGuests: Number(data.numberOfGuests),
          guestName: data.guestName,
          guestPhone: data.guestPhone,
          guestNote: data.guestNote,
          promoCode: (data.promoCode || '').trim(),
          spa: addons.spa,
          breakfast: addons.breakfast,
          extraBed: addons.extraBed,
          paymentChoice: data.paymentChoice,
          addOns: addons.airportTransfer ? [{ code: 'airport-transfer', quantity: 1 }] : []
        }
      });
      if (result.requiresPayment) {
        navigate(`/pay/${result.bookingId}`, { state: { message: result.message, kind: 'ok' } });
      } else {
        navigate('/bookings', { state: { message: result.message, kind: 'ok' } });
      }
    } catch (err) {
      if (err.status === 401) navigate('/login', { state: { from: `/rooms/${id}` } });
      else setError(err.message);
    }
  }

  async function onReview(event) {
    event.preventDefault();
    setError('');
    try {
      const result = await api(`/api/rooms/${id}/reviews`, { method: 'POST', body: new FormData(event.target) });
      setTick((value) => value + 1);
      navigate(`/rooms/${id}`, { state: { message: result.message, kind: 'ok' } });
    } catch (err) {
      setError(err.message);
    }
  }

  async function onReply(event, reviewId) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    try {
      const result = await api(`/api/rooms/reviews/${reviewId}/reply`, {
        method: 'POST',
        body: { response: data.response }
      });
      setTick((value) => value + 1);
      navigate(`/rooms/${id}`, { state: { message: result.message, kind: 'ok' } });
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <div className="lux lux-page">
      <LuxBar solid />
      <AsyncView state={state}>
      {(data) => {
        const room = data.room;
        const summary = room.reviewsSummary || { reviews: [], averageRating: 0, totalReviews: 0 };
        const includesBreakfast = (room.amenities || []).includes('Buffet sáng');
        return (
          <section className="lux-book-page">
            {error && <div className="banner">{error}</div>}
            <div className="lux-detail">
              <article className="lux-book-card">
                <Photo url={(room.imageUrls || [])[0]} />
                <p className="lux-kicker dark">{room.propertyName} · {room.area}</p>
                <h1>Phòng {room.roomNumber}</h1>
                <div className="lux-meta">
                  <span>{room.typeName}</span>
                  <span>Tối đa {room.maxGuests} khách</span>
                  {includesBreakfast && <span>Đã gồm buffet sáng</span>}
                  <strong>{money(room.basePrice)} / đêm</strong>
                </div>
                <p>{room.description}</p>
                <p className="lux-note">{room.address}</p>
                <p className="lux-note">{room.occupancyRule}</p>
                <p className="lux-amenity">{(room.amenities || []).join(' · ')}</p>
              </article>
              {!ready ? (
                <aside className="lux-book-card lux-reserve">
                  <p className="lux-note">Đang kiểm tra tài khoản...</p>
                </aside>
              ) : !user ? (
                <aside className="lux-book-card lux-reserve">
                  <p className="lux-kicker dark">HBS Reservation</p>
                  <h2>Đăng nhập để đặt phòng</h2>
                  <p>Quý khách cần đăng nhập trước khi giữ phòng. Sau khi đăng nhập, trang này mở lại để tiếp tục đặt.</p>
                  <button className="lux-gold" type="button" onClick={askLogin}>Đăng nhập</button>
                  <Link className="lux-line" to="/register" state={{ from: `/rooms/${id}` }}>Đăng ký tài khoản</Link>
                </aside>
              ) : (
              <form className="lux-book-card lux-reserve" onSubmit={onBook}>
                <p className="lux-kicker dark">HBS Reservation</p>
                <h2>Đặt phòng trực tuyến</h2>
                <div className="lux-fields">
                  <StayDates checkIn={saved.checkInDate || today()} checkOut={saved.checkOutDate || tomorrow()} />
                  <label>Số khách<input name="numberOfGuests" type="number" min="1" max={room.maxGuests} defaultValue={saved.guests || 1} required /></label>
                  <label>Họ và tên quý khách<input name="guestName" defaultValue={user?.fullName || ''} required /></label>
                  <label>Số điện thoại<input name="guestPhone" inputMode="tel" pattern="0[0-9]{9}" placeholder="0901234567" defaultValue={user?.phoneNumber || ''} required /></label>
                  <label>Yêu cầu đặc biệt<textarea name="guestNote" maxLength="500" placeholder="Đưa đón, tầng cao, kỷ niệm..."></textarea></label>
                  <label className="span-2">Mã giảm giá
                    <select name="promoCode" defaultValue="">
                      <option value="">Không áp dụng mã</option>
                      {(data.promoCodes || []).map((promo) => (
                        <option key={promo.code} value={promo.code}>{promo.code} · {promo.discountPercent > 0 ? `giảm ${promo.discountPercent}%` : `giảm ${money(promo.discountAmount)}`} · đơn từ {money(promo.minimumOrderAmount)}{promo.isNewCustomerOnly ? ' · khách mới' : ''}</option>
                      ))}
                    </select>
                  </label>
                  {data.promoLoadError && <p className="lux-note">Kho ưu đãi chưa tải được; bạn vẫn có thể đặt phòng.</p>}
                </div>
                <fieldset className="pay-choice">
                  <legend>Dịch vụ thêm</legend>
                  <label className="choice">
                    <input type="checkbox" checked={addons.spa} onChange={(event) => setAddons((current) => ({ ...current, spa: event.target.checked }))} />
                    {offer('spa') ? `${offer('spa').name} · ${money(offer('spa').price)}` : 'Spa · 300.000 ₫'}
                  </label>
                  {(data.addOns || []).filter((item) => item.code === 'airport-transfer').map((item) => (
                    <label className="choice" key={item.code}>
                      <input type="checkbox" checked={addons.airportTransfer || false} onChange={(event) => setAddons((current) => ({ ...current, airportTransfer: event.target.checked }))} />
                      {item.name} · {money(item.unitPrice)} / {item.unitLabel}
                    </label>
                  ))}
                  <label className="choice">
                    <input type="checkbox" checked={addons.breakfast} onChange={(event) => setAddons((current) => ({ ...current, breakfast: event.target.checked }))} />
                    {offer('breakfast')
                      ? `${offer('breakfast').name} · ${includesBreakfast ? 'đã gồm trong phòng' : `${money(offer('breakfast').price)} · ${offer('breakfast').unitLabel}`}`
                      : `Buffet sáng · ${includesBreakfast ? 'đã gồm trong phòng' : '180.000 ₫ / khách / đêm'}`}
                  </label>
                  {addons.breakfast && includesBreakfast && <p className="service-note">Phòng đã có suất buffet sáng rồi.</p>}
                  <label className="choice">
                    <input type="checkbox" checked={addons.extraBed} onChange={(event) => setAddons((current) => ({ ...current, extraBed: event.target.checked }))} />
                    {offer('extra-bed') ? `${offer('extra-bed').name} · ${money(offer('extra-bed').price)} · ${offer('extra-bed').unitLabel}` : 'Giường phụ · 250.000 ₫ / đêm'}
                  </label>
                </fieldset>
                <fieldset className="pay-choice">
                  <legend>Thanh toán</legend>
                  <label className="choice"><input type="radio" name="paymentChoice" value="Now" defaultChecked /> Thanh toán ngay</label>
                  <label className="choice"><input type="radio" name="paymentChoice" value="Cash" /> Tiền mặt khi nhận phòng</label>
                </fieldset>
                {preview && (
                  <div className="lux-quote">
                    <span>{preview.stayLabel} · {preview.nights} đêm · {preview.numberOfGuests} khách</span>
                    <span>Giá phòng {money(preview.roomCharge)}</span>
                    <span>Phụ thu {money(preview.surcharge)}</span>
                  {(preview.addOns || []).map((line) => <span key={line.code}>{line.name} · {line.quantity} {line.unitLabel}: {money(line.totalPrice)}</span>)}
                  {(preview.services || []).map((line) => (
                      <span key={line.name}>{line.amount > 0 ? `${line.name} ${money(line.amount)}` : line.note}</span>
                    ))}
                  {(preview.discount ?? preview.discountTotal) > 0 && <span>Giảm mã {preview.promoCode} −{money(preview.discount ?? preview.discountTotal)}</span>}
                    <strong>Tổng {money(preview.payable ?? preview.totalPrice)}</strong>
                    <span>{preview.summary}</span>
                  </div>
                )}
                <button className="lux-line" type="button" onClick={onPreview}>Xem giá</button>
                <button className="lux-gold" type="submit">Tiến hành đặt hạng phòng này</button>
              </form>
              )}
            </div>
            <section className="lux-reviews">
              <h2>Đánh giá <StarRow value={summary.averageRating} /> · {summary.totalReviews || 0} lượt</h2>
              {(summary.reviews || []).length === 0 && <p className="muted">Chưa có đánh giá.</p>}
              {(summary.reviews || []).map((review) => (
                <article className="panel" key={review.reviewId}>
                  <div className="spread">
                    <strong>{review.userName}</strong>
                    <StarRow value={review.rating} />
                  </div>
                  <p>{review.comment}</p>
                  {(review.imageUrls || []).length > 0 && <div className="review-images">{review.imageUrls.map((url) => <img key={url} src={url} alt="Ảnh khách gửi kèm đánh giá" loading="lazy" />)}</div>}
                  {review.staffResponse && <p className="muted">Phản hồi: {review.staffResponse}</p>}
                  {hasRole('Receptionist', 'Manager', 'Admin') && (
                    <form className="row" onSubmit={(event) => onReply(event, review.reviewId)}>
                      <input name="response" placeholder="Phản hồi của khách sạn" required />
                      <button className="btn small" type="submit">Gửi</button>
                    </form>
                  )}
                </article>
              ))}
              {user && data.canReview ? (
                <form className="panel stack" onSubmit={onReview}>
                  <h3>Chia sẻ trải nghiệm sau check-out</h3>
                  <StarPicker />
                  <label>Nhận xét<textarea name="comment"></textarea></label>
                  <label>Ảnh<input name="images" type="file" accept="image/*" multiple /></label>
                  <button className="btn" type="submit">Gửi đánh giá</button>
                </form>
              ) : user ? <p className="lux-note">Chỉ khách đã hoàn tất check-out mới được đánh giá phòng này.</p> : <p className="lux-note">Mời bạn <Link to="/login">đăng nhập</Link> để chia sẻ trải nghiệm.</p>}
            </section>
          </section>
        );
      }}
      </AsyncView>
    </div>
  );
}

import { useEffect, useState } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from './auth';
import { addDays, money, statusLabel, today } from './format';

export function useLoad(loader, deps) {
  const [state, setState] = useState({ loading: true, error: null, data: null });

  useEffect(() => {
    let cancelled = false;
    setState((current) => ({ ...current, loading: true, error: null }));
    Promise.resolve()
      .then(loader)
      .then((data) => {
        if (!cancelled) setState({ loading: false, error: null, data });
      })
      .catch((error) => {
        if (!cancelled) setState({ loading: false, error, data: null });
      });
    return () => {
      cancelled = true;
    };
  }, deps);

  return state;
}

export function AsyncView({ state, children }) {
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    if (state.error?.status === 401) {
      navigate('/login', {
        replace: true,
        state: { from: `${location.pathname}${location.search}` }
      });
    }
  }, [state.error, navigate, location.pathname, location.search]);

  if (state.loading) return <p className="muted">Đang tải...</p>;
  if (state.error?.status === 401) return <p className="muted">Đang chuyển tới trang đăng nhập...</p>;
  if (state.error) {
    return (
      <>
        <h1>Không tải được dữ liệu</h1>
        <p>{state.error.message}</p>
      </>
    );
  }
  return children(state.data);
}

const STAR_PATH = 'M12 2.6l2.7 5.5 6.1.9-4.4 4.3 1 6.1L12 16.8 6.6 19.4l1-6.1L3.2 9l6.1-.9L12 2.6z';

function StarIcon({ filled }) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className={filled ? '' : 'off'}>
      <path d={STAR_PATH} />
    </svg>
  );
}

export function StarRow({ value }) {
  const score = Math.max(0, Math.min(5, Number(value) || 0));
  const filled = Math.round(score);
  return (
    <span className="stars" aria-label={`${score} trên 5 sao`}>
      {[1, 2, 3, 4, 5].map((star) => <StarIcon key={star} filled={star <= filled} />)}
    </span>
  );
}

export function StarPicker({ name = 'rating', defaultValue = 5 }) {
  const [rating, setRating] = useState(defaultValue);
  const [hover, setHover] = useState(0);
  const shown = hover || rating;

  return (
    <div className="star-picker" onMouseLeave={() => setHover(0)}>
      <span>Số sao</span>
      <input type="hidden" name={name} value={rating} />
      <div>
        {[1, 2, 3, 4, 5].map((star) => (
          <button
            key={star}
            type="button"
            className={star <= shown ? 'on' : ''}
            aria-label={`${star} sao`}
            onMouseEnter={() => setHover(star)}
            onClick={() => setRating(star)}
          >
            <StarIcon filled={star <= shown} />
          </button>
        ))}
      </div>
    </div>
  );
}

export function StayDates({ checkIn, checkOut }) {
  const minIn = today();
  const initialIn = checkIn && checkIn >= minIn ? checkIn : minIn;
  const initialOutFloor = addDays(initialIn, 1);
  const initialOut = checkOut && checkOut >= initialOutFloor ? checkOut : initialOutFloor;
  const [checkInValue, setCheckInValue] = useState(initialIn);
  const [checkOutValue, setCheckOutValue] = useState(initialOut);

  useEffect(() => {
    setCheckInValue(initialIn);
    setCheckOutValue(initialOut);
  }, [initialIn, initialOut]);

  const minOut = /^\d{4}-\d{2}-\d{2}$/.test(checkInValue) ? addDays(checkInValue, 1) : initialOutFloor;

  function onCheckIn(event) {
    const next = event.target.value;
    if (next && next < minIn) {
      setCheckInValue(minIn);
      return;
    }
    setCheckInValue(next);
    if (/^\d{4}-\d{2}-\d{2}$/.test(next)) {
      const nextMinOut = addDays(next, 1);
      setCheckOutValue((current) => (!current || current < nextMinOut ? nextMinOut : current));
    }
  }

  function onCheckOut(event) {
    const next = event.target.value;
    if (next && next < minOut) {
      setCheckOutValue(minOut);
      return;
    }
    setCheckOutValue(next);
  }

  return (
    <>
      <label>
        Nhận phòng
        <input name="checkInDate" type="date" min={minIn} value={checkInValue} onChange={onCheckIn} required />
      </label>
      <label>
        Trả phòng
        <input name="checkOutDate" type="date" min={minOut} value={checkOutValue} onChange={onCheckOut} required />
      </label>
    </>
  );
}

export function TypeSelect({ types, value }) {
  return (
    <label>
      Loại phòng
      <select name="roomTypeId" defaultValue={value || ''}>
        <option value="">Tất cả</option>
        {types.map((type) => (
          <option value={type.roomTypeId} key={type.roomTypeId}>{type.typeName}</option>
        ))}
      </select>
    </label>
  );
}

export function Photo({ url }) {
  const [hidden, setHidden] = useState(!url);
  return (
    <div className="photo">
      {!hidden && <img src={url} alt="" onError={() => setHidden(true)} />}
    </div>
  );
}

export function RoomGrid({ rooms }) {
  if (!rooms?.length) return <p className="muted">Chưa có phòng phù hợp.</p>;
  return (
    <div className="grid rooms">
      {rooms.map((room) => (
        <Link className="card" to={`/rooms/${room.roomId}`} key={room.roomId}>
          <Photo url={room.primaryImageUrl} />
          <div className="body">
            <div className="spread">
              <strong>Phòng {room.roomNumber}</strong>
              <span className="pill">{room.typeName}</span>
            </div>
            <p className="muted">{room.address || 'Đà Nẵng'} · {room.maxGuests} khách</p>
            <div className="price">{money(room.basePrice)} / đêm</div>
          </div>
        </Link>
      ))}
    </div>
  );
}

export function BookingTable({ rows, actions }) {
  if (!rows?.length) return <p className="muted">Không có đơn.</p>;
  return (
    <div className="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Mã</th>
            <th>Phòng</th>
            <th>Khách</th>
            <th>Ngày</th>
            <th>Trạng thái</th>
            <th>Tiền</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {rows.map((item) => (
            <tr key={item.bookingId}>
              <td>#{item.bookingId}</td>
              <td>{item.roomNumber} · {item.typeName}</td>
              <td>{item.customerName || ''}</td>
              <td>{item.checkInDate} → {item.checkOutDate}</td>
              <td><span className="pill">{statusLabel(item.status)}</span></td>
              <td>{money(item.totalPrice)}</td>
              <td>{actions ? actions(item) : null}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function RequireAuth({ roles, children }) {
  const { user, ready } = useAuth();
  const location = useLocation();
  if (!ready) return <p className="muted">Đang tải...</p>;
  if (!user) {
    return <Navigate to="/login" replace state={{ from: `${location.pathname}${location.search}` }} />;
  }
  if (roles && !roles.includes(user.role)) return <p>Bạn không có quyền xem trang này.</p>;
  return children;
}

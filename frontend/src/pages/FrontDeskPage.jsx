import { useEffect, useState } from 'react';
import { api } from '../api';
import { useAuth } from '../auth';
import { addDays, money, statusLabel, today, tomorrow } from '../format';
import { AsyncView, StarRow, useLoad } from '../ui';

const NAV = [
  ['board', 'Quầy hôm nay'],
  ['matrix', 'Sơ đồ phòng'],
  ['phone', 'Đặt qua điện thoại'],
  ['rooms', 'Buồng phòng'],
  ['guests', 'Khách hàng']
];

const HOUSEKEEPING = [
  ['Clean', 'Sạch'],
  ['Cleaning', 'Đang dọn'],
  ['Dirty', 'Chưa dọn'],
  ['Maintenance', 'Bảo trì']
];

const PAYMENTS = [
  ['Cash', 'Tiền mặt'],
  ['Card', 'Thẻ'],
  ['Transfer', 'Chuyển khoản'],
  ['Wallet', 'Ví điện tử']
];

const NOTE = 'Phòng không hút thuốc, giường đôi, view hướng biển, dị ứng đồ ăn...';

// ─── Helpers ────────────────────────────────────────────────────────────────

function tileClass(status) {
  if (status === 'Available') return 'room-tile available';
  if (status === 'Booked' || status === 'Occupied') return 'room-tile booked';
  if (status === 'Maintenance') return 'room-tile maintenance';
  return 'room-tile other';
}

function statusIcon(status) {
  if (status === 'Available') return '✓';
  if (status === 'Booked' || status === 'Occupied') return '🛏';
  if (status === 'Maintenance') return '🔧';
  return '•';
}

// ─── Walk-in Booking Form ────────────────────────────────────────────────────

function WalkInForm({ matrixFloors, preselectedRoomId, onSuccess, onCancel }) {
  const availableRooms = (matrixFloors || []).flatMap(f =>
    f.rooms.filter(r => r.status === 'Available')
  );
  const [form, setForm] = useState({
    guestName: '',
    guestPhone: '',
    guestEmail: '',
    roomId: preselectedRoomId || (availableRooms[0]?.roomId ?? ''),
    checkInDate: today(),
    checkOutDate: addDays(today(), 1),
    adults: 1,
    children: 0,
    paymentMethod: 'Cash',
  });
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState('');

  function set(field) {
    return (e) => setForm(prev => ({ ...prev, [field]: e.target.value }));
  }

  async function submit(e) {
    e.preventDefault();
    setBusy(true);
    setErr('');
    try {
      const result = await api('/api/frontdesk/walk-in', {
        method: 'POST',
        body: {
          ...form,
          roomId: Number(form.roomId),
          adults: Number(form.adults),
          children: Number(form.children),
        },
      });
      onSuccess(result.message || 'Đặt phòng thành công!');
    } catch (ex) {
      setErr(ex.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="panel" onSubmit={submit} style={{ marginBottom: '1.5rem' }}>
      <h2 style={{ marginTop: 0 }}>Walk-in Booking</h2>
      {err && <div className="banner">{err}</div>}
      <div className="form-grid" style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
        <label>
          Tên khách *
          <input value={form.guestName} onChange={set('guestName')} required />
        </label>
        <label>
          SĐT *
          <input type="tel" value={form.guestPhone} onChange={set('guestPhone')} required />
        </label>
        <label>
          Email
          <input type="email" value={form.guestEmail} onChange={set('guestEmail')} />
        </label>
        <label>
          Phòng số *
          <select value={form.roomId} onChange={set('roomId')} required>
            {availableRooms.length === 0 && <option value="">Không có phòng trống</option>}
            {availableRooms.map(r => (
              <option key={r.roomId} value={r.roomId}>
                {r.roomNumber} – {r.typeName} ({money(r.basePrice)}/đêm)
              </option>
            ))}
          </select>
        </label>
        <label>
          Nhận phòng *
          <input type="date" value={form.checkInDate} min={today()} onChange={set('checkInDate')} required />
        </label>
        <label>
          Trả phòng *
          <input type="date" value={form.checkOutDate} min={addDays(form.checkInDate, 1)} onChange={set('checkOutDate')} required />
        </label>
        <label>
          Người lớn
          <input type="number" value={form.adults} min={1} onChange={set('adults')} required />
        </label>
        <label>
          Trẻ em
          <input type="number" value={form.children} min={0} onChange={set('children')} required />
        </label>
        <label>
          Phương thức TT
          <select value={form.paymentMethod} onChange={set('paymentMethod')}>
            <option value="Cash">Tiền mặt</option>
            <option value="Card">Thẻ</option>
          </select>
        </label>
      </div>
      <div className="modal-actions">
        <button className="btn" type="submit" disabled={busy}>{busy ? 'Đang xử lý...' : '✚ Đặt phòng'}</button>
        <button className="btn secondary" type="button" onClick={onCancel}>Hủy</button>
      </div>
    </form>
  );
}

// ─── Room Tile Modal ─────────────────────────────────────────────────────────

function RoomModal({ room, onClose, onAction, matrixFloors }) {
  const [showTransfer, setShowTransfer] = useState(false);
  const [showExtend, setShowExtend] = useState(false);
  const [showInvoice, setShowInvoice] = useState(false);
  const [transferRoomId, setTransferRoomId] = useState('');
  const [newCheckOut, setNewCheckOut] = useState('');
  const [invoice, setInvoice] = useState(null);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState('');

  const availableRooms = (matrixFloors || []).flatMap(f =>
    f.rooms.filter(r => r.status === 'Available' && r.roomId !== room.roomId)
  );

  async function handleClean() {
    setBusy(true);
    setErr('');
    try {
      const r = await api(`/api/frontdesk/rooms/${room.roomId}/clean`, { method: 'POST' });
      onAction(r.message || 'Cập nhật thành công!');
    } catch (ex) { setErr(ex.message); } finally { setBusy(false); }
  }

  async function handleCheckout() {
    setBusy(true);
    setErr('');
    try {
      const r = await api(`/api/frontdesk/check-out/${room.activeBookingId}`, { method: 'POST' });
      onAction(r.message || 'Check-out thành công!');
    } catch (ex) { setErr(ex.message); } finally { setBusy(false); }
  }

  async function handleTransfer() {
    setBusy(true);
    setErr('');
    try {
      const r = await api(`/api/frontdesk/bookings/${room.activeBookingId}/transfer-room`, {
        method: 'POST',
        body: { newRoomId: Number(transferRoomId) },
      });
      onAction(r.message || 'Đổi phòng thành công!');
    } catch (ex) { setErr(ex.message); } finally { setBusy(false); }
  }

  async function handleExtend() {
    setBusy(true);
    setErr('');
    try {
      const r = await api(`/api/frontdesk/bookings/${room.activeBookingId}/extend-stay`, {
        method: 'POST',
        body: { newCheckOutDate: newCheckOut },
      });
      onAction(r.message || 'Gia hạn thành công!');
    } catch (ex) { setErr(ex.message); } finally { setBusy(false); }
  }

  async function loadInvoice() {
    setBusy(true);
    setErr('');
    try {
      const data = await api(`/api/frontdesk/bookings/${room.activeBookingId}/invoice`);
      setInvoice(data);
      setShowInvoice(true);
    } catch (ex) { setErr(ex.message); } finally { setBusy(false); }
  }

  const isBooked = room.status === 'Booked' || room.status === 'Occupied';
  const minExtendDate = room.checkOutDate ? addDays(room.checkOutDate, 1) : addDays(today(), 1);

  return (
    <div className="modal-overlay" onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}>
      <div className="modal-box">
        {showInvoice && invoice ? (
          <>
            <style>{`@media print { .no-print { display:none!important } nav,footer,.topbar{display:none!important} body{background:white!important} }`}</style>
            <div className="invoice-container">
              <div className="invoice-header">
                <h1>HBS Luxury Hotel</h1>
                <p>123 Nguyễn Huệ, Q.1, TP.HCM &nbsp;|&nbsp; ☎ 1900-8888</p>
                <p><strong>HÓA ĐƠN #{invoice.bookingId}</strong> &nbsp;|&nbsp; {new Date().toLocaleDateString('vi-VN')}</p>
              </div>
              <table className="invoice-table">
                <tbody>
                  <tr><th>Khách</th><td>{invoice.guestName}</td></tr>
                  {invoice.guestPhone && <tr><th>SĐT</th><td>{invoice.guestPhone}</td></tr>}
                  <tr><th>Phòng</th><td>{invoice.roomNumber}</td></tr>
                  <tr><th>Nhận phòng</th><td>{invoice.checkInDate}</td></tr>
                  <tr><th>Trả phòng</th><td>{invoice.checkOutDate}</td></tr>
                </tbody>
              </table>
              <table className="invoice-table">
                <thead><tr><th>Khoản</th><th>Thành tiền</th></tr></thead>
                <tbody>
                  <tr><td>Tiền phòng cơ bản</td><td>{money(invoice.baseRoomTotal || 0)}</td></tr>
                  {invoice.extraGuestSurcharge > 0 && <tr><td>Phụ thu khách thêm</td><td>{money(invoice.extraGuestSurcharge)}</td></tr>}
                  {invoice.weekendSurcharge > 0 && <tr><td>Phụ thu cuối tuần</td><td>{money(invoice.weekendSurcharge)}</td></tr>}
                  <tr><td>VAT 10%</td><td>{money(invoice.vatAmount || (invoice.totalAmount || 0) * 0.1)}</td></tr>
                </tbody>
              </table>
              <p className="invoice-total">Tổng cộng: {money(invoice.totalAmount)}</p>
              <div className="modal-actions no-print">
                <button className="btn" onClick={() => window.print()}>🖨 In hóa đơn</button>
                <button className="btn secondary" onClick={() => setShowInvoice(false)}>Đóng</button>
              </div>
            </div>
          </>
        ) : showTransfer ? (
          <>
            <h2>Đổi phòng</h2>
            {err && <div className="banner">{err}</div>}
            <label>Phòng mới
              <select value={transferRoomId} onChange={e => setTransferRoomId(e.target.value)}>
                <option value="">-- chọn phòng --</option>
                {availableRooms.map(r => (
                  <option key={r.roomId} value={r.roomId}>{r.roomNumber} – {r.typeName}</option>
                ))}
              </select>
            </label>
            <div className="modal-actions">
              <button className="btn" onClick={handleTransfer} disabled={!transferRoomId || busy}>Xác nhận</button>
              <button className="btn secondary" onClick={() => setShowTransfer(false)}>Hủy</button>
            </div>
          </>
        ) : showExtend ? (
          <>
            <h2>Gia hạn lưu trú</h2>
            {err && <div className="banner">{err}</div>}
            <label>Ngày trả phòng mới
              <input type="date" value={newCheckOut} min={minExtendDate}
                onChange={e => setNewCheckOut(e.target.value)} />
            </label>
            <div className="modal-actions">
              <button className="btn" onClick={handleExtend} disabled={!newCheckOut || busy}>Xác nhận</button>
              <button className="btn secondary" onClick={() => setShowExtend(false)}>Hủy</button>
            </div>
          </>
        ) : (
          <>
            <h2>Phòng {room.roomNumber}</h2>
            {err && <div className="banner">{err}</div>}
            <table style={{ width: '100%', marginBottom: '0.5rem' }}>
              <tbody>
                <tr><td style={{ color: 'var(--muted)', paddingRight: '1rem' }}>Hạng</td><td>{room.typeName}</td></tr>
                <tr><td style={{ color: 'var(--muted)' }}>Tình trạng</td><td>{statusLabel(room.status)}</td></tr>
                <tr><td style={{ color: 'var(--muted)' }}>Giá/đêm</td><td>{money(room.basePrice)}</td></tr>
                {isBooked && room.guestName && <>
                  <tr><td style={{ color: 'var(--muted)' }}>Khách</td><td>{room.guestName}</td></tr>
                  {room.guestPhone && <tr><td style={{ color: 'var(--muted)' }}>SĐT</td><td>{room.guestPhone}</td></tr>}
                  <tr><td style={{ color: 'var(--muted)' }}>Nhận phòng</td><td>{room.checkInDate}</td></tr>
                  <tr><td style={{ color: 'var(--muted)' }}>Trả phòng</td><td>{room.checkOutDate}</td></tr>
                  <tr><td style={{ color: 'var(--muted)' }}>Mã đặt phòng</td><td>#{room.activeBookingId}</td></tr>
                </>}
              </tbody>
            </table>
            <div className="modal-actions">
              {isBooked && <>
                <button className="btn" onClick={handleCheckout} disabled={busy}>Check-out</button>
                <button className="btn secondary" onClick={() => setShowTransfer(true)}>Đổi phòng</button>
                <button className="btn secondary" onClick={() => setShowExtend(true)}>Gia hạn</button>
                <button className="btn secondary" onClick={loadInvoice} disabled={busy}>In hóa đơn</button>
              </>}
              {room.status === 'Maintenance' && (
                <button className="btn" onClick={handleClean} disabled={busy}>✓ Dọn phòng xong</button>
              )}
              <button className="btn secondary" onClick={onClose}>Đóng</button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}

// ─── Room Matrix Tab ─────────────────────────────────────────────────────────

function RoomMatrixTab({ matrixState, onRoomClick }) {
  return (
    <AsyncView state={matrixState}>
      {(floors) => (
        <>
          <div className="matrix-legend">
            <span className="legend-item"><span className="legend-dot" style={{ background: '#1f6b45' }} />Trống</span>
            <span className="legend-item"><span className="legend-dot" style={{ background: '#9d3b32' }} />Có khách</span>
            <span className="legend-item"><span className="legend-dot" style={{ background: '#b5812f' }} />Bảo trì</span>
            <span className="legend-item"><span className="legend-dot" style={{ background: '#6d645b' }} />Khác</span>
          </div>
          {(floors || []).map(floor => (
            <div className="room-matrix-floor" key={floor.floor}>
              <h3>Tầng {floor.floor}</h3>
              <div className="room-tiles">
                {floor.rooms.map(room => (
                  <div
                    key={room.roomId}
                    className={tileClass(room.status)}
                    onClick={() => onRoomClick(room)}
                    title={`${room.roomNumber} – ${statusLabel(room.status)}`}
                  >
                    <strong>{room.roomNumber}</strong>
                    <small>{room.typeName}</small>
                    <div>{statusIcon(room.status)}</div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </>
      )}
    </AsyncView>
  );
}

// ─── Main Page ───────────────────────────────────────────────────────────────

export function FrontDeskPage() {
  const { user, logout } = useAuth();
  const [tick, setTick] = useState(0);
  const [view, setView] = useState('board');
  const [selectedId, setSelectedId] = useState(null);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [showWalkIn, setShowWalkIn] = useState(false);
  const [preselectedRoom, setPreselectedRoom] = useState(null);
  const [selectedRoom, setSelectedRoom] = useState(null);

  const state = useLoad(() => api('/api/frontdesk'), [tick]);
  const matrixState = useLoad(() => api('/api/frontdesk/room-matrix'), [tick]);
  const matrixFloors = matrixState.data ?? [];

  function refresh(message) {
    setError('');
    setNotice(message || '');
    setSelectedRoom(null);
    setShowWalkIn(false);
    setPreselectedRoom(null);
    setTick((value) => value + 1);
  }

  function openStay(bookingId) {
    setSelectedId(bookingId);
    setView('board');
  }

  function openWalkIn(room) {
    setPreselectedRoom(room?.roomId ?? null);
    setSelectedRoom(null);
    setShowWalkIn(true);
  }

  return (
    <div className="desk-app">
      <header className="desk-top">
        <div>
          <strong>Quầy lễ tân</strong>
          <span>HBS Đà Nẵng</span>
        </div>
        <div className="desk-top-user">
          <span>{user?.fullName}</span>
          <button type="button" onClick={() => { logout(); window.location.assign('/login'); }}>Đăng xuất</button>
        </div>
      </header>
      <aside className="desk-nav">
        {NAV.map(([id, label]) => (
          <button key={id} className={view === id ? 'on' : ''} type="button" onClick={() => { setView(id); setShowWalkIn(false); }}>{label}</button>
        ))}
      </aside>
      <section className="desk-work">
        {notice && <div className="banner ok">{notice}</div>}
        {error && <div className="banner">{error}</div>}

        {showWalkIn && (
          <WalkInForm
            matrixFloors={matrixFloors}
            preselectedRoomId={preselectedRoom}
            onSuccess={(msg) => refresh(msg)}
            onCancel={() => setShowWalkIn(false)}
          />
        )}

        {view === 'matrix' && (
          <div className="desk-board" style={{ display: 'block' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
              <h2>Sơ đồ phòng</h2>
              <button className="btn small" type="button" onClick={() => setShowWalkIn(s => !s)}>
                {showWalkIn ? '✕ Đóng Walk-in' : '✚ Walk-in Booking'}
              </button>
            </div>
            <RoomMatrixTab
              matrixState={matrixState}
              onRoomClick={(room) => {
                if (room.status === 'Available') {
                  openWalkIn(room);
                } else if (room.activeBookingId) {
                  setSelectedId(room.activeBookingId);
                  setSelectedRoom(room);
                } else {
                  setSelectedRoom(room);
                }
              }}
            />
          </div>
        )}

        <AsyncView state={state}>
          {(data) => (
            <>
              {view === 'board' && (
                <>
                  <div style={{ marginBottom: '1rem' }}>
                    <button className="btn small" type="button" onClick={() => setShowWalkIn(s => !s)}>
                      {showWalkIn ? '✕ Đóng Walk-in' : '✚ Walk-in Booking'}
                    </button>
                  </div>
                  <Board
                    data={data}
                    selectedId={selectedId}
                    onOpen={setSelectedId}
                    onCancel={(id) => api(`/api/frontdesk/confirm-cancel/${id}`, { method: 'POST' }).then((result) => refresh(result.message)).catch((err) => setError(err.message))}
                  />
                </>
              )}
              {view === 'phone' && (
                <PhoneBook
                  onCreated={(result) => { openStay(result.bookingId); refresh(result.message); }}
                  onError={setError}
                />
              )}
              {view === 'rooms' && <Rooms data={data} onDone={refresh} onError={setError} />}
              {view === 'guests' && <Guests onDone={refresh} onError={setError} />}
            </>
          )}
        </AsyncView>
      </section>
      <Bill
        bookingId={selectedId}
        services={state.data?.services || []}
        tick={tick}
        onDone={refresh}
        onError={setError}
      />

      {selectedRoom && (
        <RoomModal
          room={selectedRoom}
          matrixFloors={matrixFloors}
          onClose={() => setSelectedRoom(null)}
          onAction={refresh}
        />
      )}
    </div>
  );
}

function Board({ data, selectedId, onOpen, onCancel }) {
  return (
    <div className="desk-board">
      <section>
        <h2>Check-in · khách đến</h2>
        <StayList rows={data.arrivals} selectedId={selectedId} action="Check-in" onOpen={onOpen} />
      </section>
      <section>
        <h2>Check-out · khách đang ở</h2>
        <StayList rows={data.checkedIn} selectedId={selectedId} action="Check-out" onOpen={onOpen} />
      </section>
      <section>
        <h2>Khách trả phòng hôm nay</h2>
        <StayList rows={data.departures} selectedId={selectedId} action="Xem bill" onOpen={onOpen} />
      </section>
      {(data.cancelRequests || []).length > 0 && (
        <section>
          <h2>Yêu cầu hủy</h2>
          {(data.cancelRequests || []).map((item) => (
            <article className="stay-card" key={item.bookingId}>
              <div>
                <strong>#{item.bookingId} · {item.guestName || item.customerName}</strong>
                <p>{item.propertyName} · phòng {item.roomNumber}</p>
                <p>{item.cancelReason}</p>
              </div>
              <button className="btn small" type="button" onClick={() => onCancel(item.bookingId)}>Xác nhận hủy</button>
            </article>
          ))}
        </section>
      )}
      {(data.reviews || []).length > 0 && (
        <section>
          <h2>Đánh giá mới</h2>
          {(data.reviews || []).slice(0, 3).map((review) => (
            <article className="stay-card" key={review.reviewId}>
              <div>
                <strong>Phòng {review.roomNumber} · {review.userName}</strong>
                <StarRow value={review.rating} />
                <p>{review.comment}</p>
              </div>
            </article>
          ))}
        </section>
      )}
    </div>
  );
}

function StayList({ rows, selectedId, action, onOpen }) {
  if (!rows?.length) return <p className="desk-empty">Không có khách.</p>;
  return rows.map((item) => (
    <article className={`stay-card ${selectedId === item.bookingId ? 'on' : ''}`} key={item.bookingId}>
      <div>
        <strong>{item.guestName || item.customerName || `Đơn #${item.bookingId}`}</strong>
        <p>{item.guestPhone || item.customerPhone || 'Chưa có số điện thoại'}</p>
        <p>{item.propertyName} · phòng {item.roomNumber}</p>
        <p>{item.checkInDate} → {item.checkOutDate} · {statusLabel(item.status)}</p>
        {item.guestNote && <p>{item.guestNote}</p>}
      </div>
      <button className="btn" type="button" onClick={() => onOpen(item.bookingId)}>{action}</button>
    </article>
  ));
}

function Bill({ bookingId, services, tick, onDone, onError }) {
  const [folio, setFolio] = useState(null);
  const [rooms, setRooms] = useState([]);
  const [roomId, setRoomId] = useState('');
  const [slipOpen, setSlipOpen] = useState(false);

  useEffect(() => {
    if (!bookingId) {
      setFolio(null);
      setSlipOpen(false);
      return undefined;
    }
    let cancel = false;
    api(`/api/frontdesk/bookings/${bookingId}/folio`)
      .then((data) => { if (!cancel) setFolio(data); })
      .catch((err) => { if (!cancel) onError(err.message); });
    return () => { cancel = true; };
  }, [bookingId, tick]);

  useEffect(() => {
    if (!folio) return undefined;
    let cancel = false;
    api(`/api/frontdesk/availability?checkIn=${folio.checkInDate}&checkOut=${folio.checkOutDate}&ignoreBookingId=${folio.bookingId}`)
      .then((data) => { if (!cancel) setRooms(data); })
      .catch(() => { if (!cancel) setRooms([]); });
    return () => { cancel = true; };
  }, [folio?.bookingId, folio?.checkInDate, folio?.checkOutDate]);

  async function post(path, body) {
    try {
      const result = await api(path, { method: 'POST', body });
      onDone(result.message);
    } catch (err) {
      onError(err.message);
    }
  }

  if (!folio) {
    return (
      <aside className="bill">
        <p className="eyebrow">Bill</p>
        <h2>Chọn một khách</h2>
        <p>Bill và số tiền khách cần thanh toán hiện ở đây khi bạn bấm Check-in hoặc Check-out.</p>
      </aside>
    );
  }

  const due = Number(folio.due || 0);
  const roomNet = Math.max(0, Number(folio.roomCharge || 0) - Number(folio.discounts || 0));
  const roomPaid = typeof folio.roomPaid === 'boolean' ? folio.roomPaid : Number(folio.paid || 0) >= roomNet;
  const extraDue = Math.max(due, 0);
  const settled = extraDue <= 0;
  const paymentNote = folio.paymentNote || (roomPaid
    ? 'Khách đã thanh toán tiền phòng. Dịch vụ phát sinh ghi thêm vào bill. Chỉ thu thêm số tiền bên dưới.'
    : 'Khách chưa thanh toán đủ tiền phòng. Bill gồm tiền phòng còn lại và dịch vụ.');
  const servicesLines = (folio.lines || []).filter((line) => line.category === 'Service' || line.category === 'Minibar' || line.category === 'Damage');
  const minibar = services.filter((item) => item.name.startsWith('Mini-bar'));
  const open = folio.status === 'Confirmed' || folio.status === 'Pending' || folio.status === 'CheckedIn';

  return (
    <aside className="bill" id="folio">
      <p className="eyebrow">Bill #{folio.bookingId}</p>
      <h2>{folio.guestName}</h2>
      <p>{folio.guestPhone}</p>
      <p>{folio.propertyName} · phòng {folio.roomNumber}</p>
      <p>{folio.checkInDate} → {folio.checkOutDate} · {statusLabel(folio.status)}</p>
      {folio.guestNote && <p>{folio.guestNote}</p>}

      <p className={`pay-note ${roomPaid ? 'paid' : 'unpaid'}`}>{paymentNote}</p>
      <div className="due-box">
        <span>{due < 0 ? 'Cần hoàn cho khách' : roomPaid ? 'Số tiền cần thanh toán thêm' : 'Số tiền khách cần thanh toán'}</span>
        <strong>{money(Math.abs(due))}</strong>
      </div>
      <ul className="folio-lines">
        <li>
          <span>Tiền phòng {roomPaid ? '· đã thanh toán' : '· chưa thanh toán đủ'}</span>
          <span>{money(folio.roomCharge)}</span>
        </li>
        {servicesLines.length === 0 && <li><span>Dịch vụ</span><span>{money(0)}</span></li>}
        {servicesLines.map((line, index) => (
          <li key={`svc-${index}`}>
            <span>{line.description}{line.quantity > 1 ? ` × ${line.quantity}` : ''}</span>
            <span>{money(line.amount)}</span>
          </li>
        ))}
        <li><span>Giảm giá</span><span>− {money(folio.discounts)}</span></li>
        <li><span>Đã thu / tiền cọc</span><span>− {money(folio.paid)}</span></li>
      </ul>
      <button className="btn small secondary" type="button" onClick={() => setSlipOpen(true)}>Xem phiếu</button>
      {slipOpen && (
        <div className="slip-back" onClick={() => setSlipOpen(false)}>
          <article className="slip" onClick={(event) => event.stopPropagation()}>
            <p className="eyebrow">Phiếu điện tử</p>
            <h2>Phiếu thanh toán #{folio.bookingId}</h2>
            <p>{folio.guestName} · {folio.guestPhone}</p>
            <p>{folio.propertyName} · phòng {folio.roomNumber}</p>
            <p>{folio.checkInDate} → {folio.checkOutDate}</p>
            <ul className="folio-lines">
              <li>
                <span>Tiền phòng</span>
                <span>{money(folio.roomCharge)}</span>
              </li>
              <li>
                <span>{roomPaid ? 'Xác nhận: đã thanh toán tiền phòng' : 'Xác nhận: chưa thanh toán đủ tiền phòng'}</span>
                <span></span>
              </li>
              {servicesLines.map((line, index) => (
                <li key={`slip-${index}`}>
                  <span>{line.description}{line.quantity > 1 ? ` × ${line.quantity}` : ''}</span>
                  <span>{money(line.amount)}</span>
                </li>
              ))}
              <li><span>Giảm giá</span><span>− {money(folio.discounts)}</span></li>
              <li><span>Đã thu</span><span>− {money(folio.paid)}</span></li>
            </ul>
            <p>{roomPaid ? 'Số tiền cần thanh toán thêm' : 'Số tiền khách cần thanh toán'}</p>
            <strong className="slip-amount">{money(extraDue)}</strong>
            <p className={`stamp ${settled ? 'paid' : 'unpaid'}`}>{settled ? 'ĐÃ THANH TOÁN' : 'CHƯA THANH TOÁN'}</p>
            <button className="btn" type="button" onClick={() => setSlipOpen(false)}>Đóng phiếu</button>
          </article>
        </div>
      )}

      {folio.status === 'Confirmed' && (
        <form onSubmit={(event) => {
          event.preventDefault();
          const data = Object.fromEntries(new FormData(event.target));
          post(`/api/frontdesk/check-in/${folio.bookingId}`, data);
        }}>
          <h3>Check-in</h3>
          <label>Xác nhận tên<input name="guestName" defaultValue={folio.guestName} required /></label>
          <label>Xác nhận số điện thoại<input name="guestPhone" defaultValue={folio.guestPhone} required pattern="0[0-9]{9}" /></label>
          <button className="btn" type="submit">Check-in cho khách</button>
        </form>
      )}

      {folio.status === 'CheckedIn' && (
        <>
          <form onSubmit={(event) => {
            event.preventDefault();
            const data = Object.fromEntries(new FormData(event.target));
            const offer = services.find((item) => item.name === data.service);
            post(`/api/frontdesk/bookings/${folio.bookingId}/charges`, {
              category: 'Service',
              description: data.service,
              quantity: Number(data.quantity),
              unitPrice: offer ? offer.price : 0
            });
          }}>
            <h3>Dịch vụ phát sinh</h3>
            <label>Dịch vụ
              <select name="service" defaultValue={services[0]?.name || ''}>
                {services.map((item) => <option key={item.name} value={item.name}>{item.name} · {money(item.price)}</option>)}
              </select>
            </label>
            <label>Số lượng<input name="quantity" type="number" min="1" defaultValue="1" /></label>
            <button className="btn small" type="submit">Cộng vào bill</button>
          </form>
          <form onSubmit={(event) => {
            event.preventDefault();
            const data = new FormData(event.target);
            post(`/api/frontdesk/check-out/${folio.bookingId}`, {
              condition: data.get('condition'),
              damageNote: data.get('damageNote'),
              paymentMethod: data.get('paymentMethod'),
              minibar: minibar.map((item) => ({
                name: item.name,
                quantity: Number(data.get(item.name) || 0),
                unitPrice: item.price
              })).filter((item) => item.quantity > 0)
            });
          }}>
            <h3>Check-out</h3>
            <label>Tình trạng phòng
              <select name="condition" defaultValue="Dirty">
                <option value="Dirty">Chưa dọn</option>
                <option value="Clean">Sạch</option>
                <option value="Maintenance">Bảo trì</option>
              </select>
            </label>
            {minibar.map((item) => (
              <label key={item.name}>{item.name}<input name={item.name} type="number" min="0" defaultValue="0" /></label>
            ))}
            <label>Đồ hư hỏng<textarea name="damageNote" placeholder="Bóng đèn, điều hòa, vòi sen..." /></label>
            <label>Thu bằng
              <select name="paymentMethod" defaultValue="Cash">
                {PAYMENTS.map(([id, label]) => <option key={id} value={id}>{label}</option>)}
              </select>
            </label>
            <button className="btn" type="submit">
              {roomPaid
                ? (extraDue > 0 ? `Check-out và thu thêm ${money(extraDue)}` : 'Check-out, không còn phải thu')
                : `Check-out và thu ${money(extraDue)}`}
            </button>
          </form>
        </>
      )}

      {open && (
        <form onSubmit={(event) => { event.preventDefault(); post(`/api/frontdesk/bookings/${folio.bookingId}/change-room`, { roomId: Number(roomId) }); }}>
          <h3>Đổi phòng</h3>
          <label>Phòng trống
            <select value={roomId} onChange={(event) => setRoomId(event.target.value)}>
              <option value="">Chọn phòng</option>
              {rooms.map((room) => <option key={room.roomId} value={room.roomId}>{room.propertyName} · {room.roomNumber} · {room.stayLabel}</option>)}
            </select>
          </label>
          <button className="btn small" type="submit">Đổi phòng</button>
        </form>
      )}
    </aside>
  );
}

function PhoneBook({ onCreated, onError }) {
  const [checkIn, setCheckIn] = useState(today());
  const [checkOut, setCheckOut] = useState(tomorrow());
  const [rooms, setRooms] = useState([]);

  useEffect(() => {
    if (checkOut <= checkIn) return undefined;
    let cancel = false;
    api(`/api/frontdesk/availability?checkIn=${checkIn}&checkOut=${checkOut}`)
      .then((data) => { if (!cancel) setRooms(data); })
      .catch((err) => { if (!cancel) onError(err.message); });
    return () => { cancel = true; };
  }, [checkIn, checkOut]);

  async function onSubmit(event) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    try {
      const result = await api('/api/frontdesk/bookings', {
        method: 'POST',
        body: {
          guestName: data.guestName,
          guestPhone: data.guestPhone,
          email: data.email,
          roomId: Number(data.roomId),
          checkInDate: checkIn,
          checkOutDate: checkOut,
          numberOfGuests: Number(data.numberOfGuests),
          guestNote: data.guestNote,
          promoCode: data.promoCode,
          deposit: Number(data.deposit || 0)
        }
      });
      onCreated(result);
    } catch (err) {
      onError(err.message);
    }
  }

  return (
    <form className="phone-book" onSubmit={onSubmit}>
      <p className="eyebrow">Điện thoại</p>
      <h2>Tạo đơn cho khách gọi điện</h2>
      <p>Ghi tên, số điện thoại, phòng trống và tiền cọc. Bill hiện bên phải ngay sau khi tạo.</p>
      <div className="desk-grid">
        <label>Tên người nhận phòng<input name="guestName" required minLength={2} /></label>
        <label>Số điện thoại<input name="guestPhone" required pattern="0[0-9]{9}" /></label>
        <label>Email<input name="email" type="email" /></label>
        <label>Số khách<input name="numberOfGuests" type="number" min="1" defaultValue="1" required /></label>
        <label>Nhận phòng<input type="date" value={checkIn} min={today()} onChange={(event) => setCheckIn(event.target.value)} required /></label>
        <label>Trả phòng<input type="date" value={checkOut} min={addDays(checkIn, 1)} onChange={(event) => setCheckOut(event.target.value)} required /></label>
        <label>Phòng trống
          <select name="roomId" required defaultValue="">
            <option value="" disabled>Chọn phòng</option>
            {rooms.map((room) => (
              <option key={room.roomId} value={room.roomId}>
                {room.propertyName} · {room.roomNumber} · {room.stayLabel} · {money(room.basePrice)}
              </option>
            ))}
          </select>
        </label>
        <label>Mã giảm giá<input name="promoCode" placeholder="WELCOME10 hoặc GIAM100" /></label>
        <label>Tiền cọc<input name="deposit" type="number" min="0" step="1000" defaultValue="0" /></label>
      </div>
      <label>Yêu cầu đặc biệt<textarea name="guestNote" placeholder={NOTE} /></label>
      <button className="btn" type="submit">Tạo đơn</button>
    </form>
  );
}

function Rooms({ data, onDone, onError }) {
  async function save(roomId, status) {
    try {
      const result = await api(`/api/frontdesk/rooms/${roomId}/housekeeping`, { method: 'POST', body: { status } });
      onDone(result.message);
    } catch (err) {
      onError(err.message);
    }
  }

  async function report(event) {
    event.preventDefault();
    const form = Object.fromEntries(new FormData(event.target));
    try {
      const result = await api(`/api/frontdesk/rooms/${form.roomId}/issues`, { method: 'POST', body: { description: form.description } });
      event.target.reset();
      onDone(result.message);
    } catch (err) {
      onError(err.message);
    }
  }

  return (
    <div className="desk-board">
      <form className="phone-book" onSubmit={report}>
        <h2>Báo sự cố phòng</h2>
        <label>Phòng
          <select name="roomId" required defaultValue="">
            <option value="" disabled>Chọn phòng</option>
            {(data.rooms || []).map((room) => <option key={room.roomId} value={room.roomId}>{room.propertyName} · {room.roomNumber}</option>)}
          </select>
        </label>
        <label>Sự cố<textarea name="description" required placeholder="Bóng đèn, điều hòa, vòi sen..." /></label>
        <button className="btn small" type="submit">Báo sự cố</button>
      </form>
      {(data.issues || []).map((issue) => (
        <article className="stay-card" key={issue.issueId}>
          <div><strong>Phòng {issue.roomNumber}</strong><p>{issue.description}</p></div>
          <button className="btn small" type="button" onClick={() => api(`/api/frontdesk/issues/${issue.issueId}/resolve`, { method: 'POST' }).then((result) => onDone(result.message)).catch((err) => onError(err.message))}>Đã xử lý</button>
        </article>
      ))}
      <div className="table-wrap">
        <table>
          <thead><tr><th>Cơ sở</th><th>Phòng</th><th>Loại</th><th>Tình trạng</th></tr></thead>
          <tbody>
            {(data.rooms || []).map((room) => (
              <tr key={room.roomId}>
                <td>{room.propertyName}</td>
                <td>{room.roomNumber}</td>
                <td>{room.stayLabel}</td>
                <td>
                  <select value={room.housekeepingStatus} onChange={(event) => save(room.roomId, event.target.value)}>
                    {HOUSEKEEPING.map(([id, label]) => <option key={id} value={id}>{label}</option>)}
                  </select>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function Guests({ onDone, onError }) {
  const [query, setQuery] = useState('');
  const [guests, setGuests] = useState([]);

  function search(event) {
    event?.preventDefault();
    api(`/api/frontdesk/customers?q=${encodeURIComponent(query)}`)
      .then(setGuests)
      .catch((err) => onError(err.message));
  }

  useEffect(() => { search(); }, []);

  async function save(event, userId) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    try {
      const result = await api(`/api/frontdesk/customers/${userId}`, { method: 'PUT', body: data });
      onDone(result.message);
      search();
    } catch (err) {
      onError(err.message);
    }
  }

  return (
    <div className="desk-board">
      <form className="desk-search" onSubmit={search}>
        <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Tên, số điện thoại hoặc email" />
        <button className="btn" type="submit">Tra cứu</button>
      </form>
      {guests.map((guest) => (
        <article className="stay-card" key={guest.userId}>
          <div>
            <strong>{guest.fullName}</strong>
            <p>{guest.stayCount >= 5 ? 'VIP' : guest.stayCount >= 2 ? 'Khách quen' : 'Khách mới'} · {guest.stayCount} lần lưu trú</p>
            <form className="desk-grid" onSubmit={(event) => save(event, guest.userId)}>
              <label>Tên<input name="fullName" defaultValue={guest.fullName} required /></label>
              <label>Điện thoại<input name="phoneNumber" defaultValue={guest.phoneNumber || ''} /></label>
              <label>Email<input name="email" type="email" defaultValue={guest.email} required /></label>
              <button className="btn small" type="submit">Lưu</button>
            </form>
            {(guest.history || []).map((item) => (
              <p key={item.bookingId}>#{item.bookingId} · {item.propertyName} {item.roomNumber} · {item.checkInDate} → {item.checkOutDate} · {statusLabel(item.status)}</p>
            ))}
          </div>
        </article>
      ))}
    </div>
  );
}

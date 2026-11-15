export const STATUS = {
  Pending: 'Chờ thanh toán',
  Confirmed: 'Đã xác nhận',
  CheckedIn: 'Đã nhận phòng',
  CheckedOut: 'Đã trả phòng',
  CancelRequested: 'Chờ xác nhận hủy',
  Cancelled: 'Đã hủy',
  Available: 'Trống',
  Booked: 'Đã đặt',
  Maintenance: 'Bảo trì',
  Dirty: 'Chờ dọn phòng',
  Occupied: 'Có khách',
};

export function money(value) {
  return `${new Intl.NumberFormat('vi-VN').format(value || 0)} ₫`;
}

export function isoDate(date) {
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 10);
}

export function today() {
  return isoDate(new Date());
}

export function tomorrow() {
  return addDays(today(), 1);
}

export function addDays(iso, days) {
  const date = new Date(`${iso}T00:00:00`);
  date.setDate(date.getDate() + days);
  return isoDate(date);
}

export function statusLabel(status) {
  return STATUS[status] || status || '';
}

export function nightsBetween(checkIn, checkOut) {
  const a = new Date(`${checkIn}T00:00:00`);
  const b = new Date(`${checkOut}T00:00:00`);
  return Math.max(0, Math.round((b - a) / 86400000));
}

export function searchParams(data) {
  const params = new URLSearchParams({
    checkInDate: data.checkInDate,
    checkOutDate: data.checkOutDate
  });
  if (data.guests) params.set('guests', data.guests);
  if (data.roomTypeId) params.set('roomTypeId', data.roomTypeId);
  if (data.propertyId) params.set('propertyId', data.propertyId);
  return params;
}

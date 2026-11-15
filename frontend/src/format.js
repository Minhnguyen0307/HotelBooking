export const STATUS = {
  Pending: 'Chờ thanh toán',
  Confirmed: 'Đã xác nhận',
  CheckedIn: 'Đã nhận phòng',
  CheckedOut: 'Đã trả phòng',
  CancelRequested: 'Chờ xác nhận hủy',
  Cancelled: 'Đã hủy',
  Available: 'Trống',
  Booked: 'Đã đặt',
  Maintenance: 'Bảo trì'
};

export function money(value) {
  return `${new Intl.NumberFormat('vi-VN').format(value || 0)} ₫`;
}

export function dateTime(value) {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';
  return new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'medium',
    timeStyle: 'short'
  }).format(date);
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

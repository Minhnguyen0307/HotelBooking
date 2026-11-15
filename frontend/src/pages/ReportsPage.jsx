import { useState } from 'react';
import { api, downloadFile } from '../api';
import { money, today } from '../format';
import { AsyncView, useLoad } from '../ui';

const periods = [
  ['day', 'Ngày'],
  ['week', 'Tuần'],
  ['month', 'Tháng']
];

export function ReportsPage() {
  const [error, setError] = useState('');
  const [period, setPeriod] = useState('day');
  const [date, setDate] = useState(today());
  const state = useLoad(() => api('/api/reports'), []);
  const revenue = useLoad(() => api(`/api/reports/revenue?period=${period}&date=${date}`), [period, date]);
  const bills = useLoad(() => api('/api/reports/bills'), []);

  async function download(path, file) {
    setError('');
    try {
      await downloadFile(path, file);
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <AsyncView state={state}>
      {(data) => (
        <>
          <header className="page-head">
            <p className="eyebrow">Quản trị</p>
            <div className="spread">
              <h1>Báo cáo</h1>
              <button className="btn small secondary" type="button" onClick={() => download('/api/reports/revenue.csv', 'BaoCaoDoanhThu.csv')}>Tải CSV</button>
            </div>
          </header>
          {error && <div className="banner">{error}</div>}
          <section className="panel stack">
            <h2>Doanh thu</h2>
            <div className="row">
              {periods.map(([value, label]) => (
                <button key={value} className={`btn small${period === value ? '' : ' secondary'}`} type="button" onClick={() => setPeriod(value)}>{label}</button>
              ))}
              <input type="date" value={date} onChange={(event) => setDate(event.target.value)} />
            </div>
            <AsyncView state={revenue}>
              {(item) => (
                <>
                  <p>{item.from} → {item.to} · {item.paymentCount} giao dịch</p>
                  <strong>Tổng {money(item.total)}</strong>
                  <div className="table-wrap">
                    <table>
                      <thead><tr><th>{item.period === 'day' ? 'Khoản thu' : 'Ngày'}</th><th>Số giao dịch</th><th>Doanh thu</th></tr></thead>
                      <tbody>
                        {(item.buckets || []).length === 0 && <tr><td colSpan="3">Chưa có doanh thu trong kỳ này.</td></tr>}
                        {(item.buckets || []).map((bucket, index) => (
                          <tr key={`${bucket.label}-${index}`}>
                            <td>{bucket.label}</td><td>{bucket.count}</td><td>{money(bucket.amount)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </>
              )}
            </AsyncView>
          </section>
          <h2>Hóa đơn đã chốt</h2>
          <AsyncView state={bills}>
            {(rows) => (
              <div className="table-wrap">
                <table>
                  <thead><tr><th>Đơn</th><th>Khách</th><th>Phòng</th><th>Đã thu</th><th>Người xác nhận</th><th>Lúc</th></tr></thead>
                  <tbody>
                    {rows.length === 0 && <tr><td colSpan="6">Chưa có hóa đơn đã chốt.</td></tr>}
                    {rows.map((bill) => (
                      <tr key={bill.bookingId}>
                        <td>#{bill.bookingId}</td>
                        <td>{bill.guestName}</td>
                        <td>{bill.roomNumber}</td>
                        <td>{money(bill.collected)}</td>
                        <td>{bill.confirmedBy || 'Chưa ghi nhận'}</td>
                        <td>{bill.confirmedAt ? new Date(bill.confirmedAt).toLocaleString('vi-VN') : ''}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </AsyncView>
          <h2>Công suất phòng</h2>
          <div className="table-wrap">
            <table>
              <thead><tr><th>Phòng</th><th>Lượt đặt</th><th>Số đêm</th></tr></thead>
              <tbody>
                {(data.occupancy || []).length === 0 && <tr><td colSpan="3">Chưa có dữ liệu</td></tr>}
                {(data.occupancy || []).map((item) => (
                  <tr key={item.roomNumber}>
                    <td>{item.roomNumber}</td><td>{item.totalBookings}</td><td>{item.totalNightsBooked ?? 0}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </AsyncView>
  );
}

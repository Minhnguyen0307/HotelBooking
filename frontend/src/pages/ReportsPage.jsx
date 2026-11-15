import { useState } from 'react';
import { api, downloadFile } from '../api';
import { money } from '../format';
import { AsyncView, useLoad } from '../ui';

export function ReportsPage() {
  const [error, setError] = useState('');
  const state = useLoad(() => api('/api/reports'), []);

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
          <div className="spread">
            <h1>Báo cáo</h1>
            <div className="row">
              <button className="btn small" type="button" onClick={() => download('/api/reports/revenue.csv', 'BaoCaoDoanhThu.csv')}>CSV doanh thu</button>
              <button className="btn small secondary" type="button" onClick={() => download('/api/reports/occupancy.csv', 'BaoCaoTanSuatPhong.csv')}>CSV công suất</button>
            </div>
          </div>
          {error && <div className="banner">{error}</div>}
          <h2>Doanh thu theo tháng</h2>
          <div className="table-wrap">
            <table>
              <thead><tr><th>Năm</th><th>Tháng</th><th>Doanh thu</th><th>Đơn đã thanh toán</th></tr></thead>
              <tbody>
                {(data.revenue || []).length === 0 && <tr><td colSpan="4">Chưa có dữ liệu</td></tr>}
                {(data.revenue || []).map((item) => (
                  <tr key={`${item.year}-${item.month}`}>
                    <td>{item.year}</td><td>{item.month}</td><td>{money(item.totalRevenue)}</td><td>{item.totalBookingsPaid}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
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

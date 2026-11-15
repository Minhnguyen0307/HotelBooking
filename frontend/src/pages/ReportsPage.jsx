import { useState, useEffect, useRef } from 'react';
import { api, downloadFile } from '../api';
import { money } from '../format';
import { AsyncView, useLoad } from '../ui';
import { Chart, registerables } from 'chart.js';
Chart.register(...registerables);

// ─── Chart hooks ─────────────────────────────────────────────────────────────

function useChart(canvasRef, buildConfig, deps) {
  useEffect(() => {
    if (!canvasRef.current) return;
    const ctx = canvasRef.current.getContext('2d');
    const chart = new Chart(ctx, buildConfig());
    return () => chart.destroy();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);
}

// ─── Revenue Line Chart ───────────────────────────────────────────────────────

function RevenueChart({ revenue }) {
  const ref = useRef(null);
  const last12 = [...(revenue || [])].slice(-12);
  const labels = last12.map(d => `Tháng ${d.month}/${d.year}`);
  const values = last12.map(d => +(d.totalRevenue / 1_000_000).toFixed(2));

  useChart(ref, () => ({
    type: 'line',
    data: {
      labels,
      datasets: [{
        label: 'Doanh thu (triệu ₫)',
        data: values,
        borderColor: '#1f4d45',
        backgroundColor: 'rgba(31,77,69,0.1)',
        tension: 0.3,
        fill: true,
        pointRadius: 4,
        pointBackgroundColor: '#1f4d45',
      }],
    },
    options: {
      responsive: true,
      plugins: { legend: { position: 'top' } },
      scales: {
        y: {
          beginAtZero: true,
          title: { display: true, text: 'Triệu VND' },
        },
      },
    },
  }), [revenue]);

  return (
    <div className="panel" style={{ marginBottom: '1rem' }}>
      <h2 style={{ marginTop: 0 }}>Doanh thu theo tháng (12 tháng gần nhất)</h2>
      <canvas ref={ref} height={120} />
    </div>
  );
}

// ─── Doughnut – Room Type ─────────────────────────────────────────────────────

function RoomTypeDoughnut({ chartData }) {
  const ref = useRef(null);
  const stats = chartData?.roomTypeStats || [];
  const labels = stats.map(s => s.typeName);
  const values = stats.map(s => s.totalBookings);
  const colors = ['#1f4d45', '#c4a574', '#9d3b32', '#1a2b4c', '#6d645b'];

  useChart(ref, () => ({
    type: 'doughnut',
    data: {
      labels,
      datasets: [{
        data: values,
        backgroundColor: colors.slice(0, labels.length),
        borderWidth: 2,
        borderColor: '#fff',
      }],
    },
    options: {
      responsive: true,
      plugins: { legend: { position: 'bottom' }, title: { display: true, text: 'Tỷ lệ đặt phòng theo hạng phòng' } },
    },
  }), [chartData]);

  return (
    <div className="panel" style={{ flex: 1 }}>
      <h2 style={{ marginTop: 0, fontSize: '1rem' }}>Hạng phòng</h2>
      <canvas ref={ref} />
    </div>
  );
}

// ─── Occupancy Gauge ──────────────────────────────────────────────────────────

function OccupancyBar({ chartData }) {
  const ref = useRef(null);
  const pct = chartData?.occupancyRatePercent ?? 0;
  const color = pct > 70 ? '#1f6b45' : pct > 40 ? '#b5812f' : '#9d3b32';

  useChart(ref, () => ({
    type: 'bar',
    data: {
      labels: ['Công suất phòng'],
      datasets: [{
        label: `${pct.toFixed(1)}%`,
        data: [pct],
        backgroundColor: color,
        borderRadius: 6,
      }],
    },
    options: {
      indexAxis: 'y',
      responsive: true,
      plugins: { legend: { display: false } },
      scales: {
        x: { min: 0, max: 100, title: { display: true, text: '%' } },
      },
    },
  }), [chartData]);

  return (
    <div className="panel" style={{ flex: 1 }}>
      <h2 style={{ marginTop: 0, fontSize: '1rem' }}>Công suất phòng</h2>
      <canvas ref={ref} height={80} />
      <p style={{ textAlign: 'center', fontWeight: 700, fontSize: '1.5rem', color }}>
        {pct.toFixed(1)}%
      </p>
    </div>
  );
}

// ─── Main Page ────────────────────────────────────────────────────────────────

export function ReportsPage() {
  const [error, setError] = useState('');
  const state = useLoad(() => api('/api/reports'), []);
  const chartState = useLoad(() => api('/api/reports/charts'), []);

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
              <button className="btn small" type="button" onClick={() => download('/api/reports/revenue.csv', 'BaoCaoDoanhThu.csv')}>
                CSV doanh thu
              </button>
              <button className="btn small secondary" type="button" onClick={() => download('/api/reports/occupancy.csv', 'BaoCaoTanSuatPhong.csv')}>
                CSV công suất
              </button>
            </div>
          </div>
          {error && <div className="banner">{error}</div>}

          {/* Charts section */}
          <RevenueChart revenue={data.revenue} />

          <div style={{ display: 'flex', gap: '1rem', marginBottom: '1rem', flexWrap: 'wrap' }}>
            <AsyncView state={chartState}>
              {(cd) => (
                <>
                  <RoomTypeDoughnut chartData={cd} />
                  <OccupancyBar chartData={cd} />
                </>
              )}
            </AsyncView>
          </div>

          {/* Revenue table */}
          <h2>Doanh thu theo tháng</h2>
          <div className="table-wrap">
            <table>
              <thead><tr><th>Năm</th><th>Tháng</th><th>Doanh thu</th><th>Đơn đã thanh toán</th></tr></thead>
              <tbody>
                {(data.revenue || []).length === 0 && <tr><td colSpan="4">Chưa có dữ liệu</td></tr>}
                {(data.revenue || []).map((item) => (
                  <tr key={`${item.year}-${item.month}`}>
                    <td>{item.year}</td><td>{item.month}</td>
                    <td>{money(item.totalRevenue)}</td><td>{item.totalBookingsPaid}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Occupancy table */}
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

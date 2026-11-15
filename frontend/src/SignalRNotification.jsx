import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import { useAuth } from './auth';
import { money } from './format';

function playChime() {
  try {
    const AudioContext = window.AudioContext || window.webkitAudioContext;
    if (!AudioContext) return;
    const ctx = new AudioContext();
    const notes = [523.25, 659.25, 783.99]; // C5, E5, G5
    notes.forEach((freq, idx) => {
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.type = 'sine';
      osc.frequency.setValueAtTime(freq, ctx.currentTime + idx * 0.12);
      gain.gain.setValueAtTime(0.2, ctx.currentTime + idx * 0.12);
      gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + idx * 0.12 + 0.6);
      osc.connect(gain);
      gain.connect(ctx.destination);
      osc.start(ctx.currentTime + idx * 0.12);
      osc.stop(ctx.currentTime + idx * 0.12 + 0.7);
    });
  } catch {
    // Audio autoplay policy
  }
}

export function SignalRNotification() {
  const { user, hasRole } = useAuth();
  const navigate = useNavigate();
  const [alert, setAlert] = useState(null);

  useEffect(() => {
    // Chỉ lễ tân, quản lý và admin mới nhận thông báo real-time
    if (!user || !hasRole('Receptionist', 'Manager', 'Admin')) return;

    let connection;
    try {
      connection = new signalR.HubConnectionBuilder()
        .withUrl('/roomHub')
        .withAutomaticReconnect([0, 2000, 5000, 10000])
        .build();

      connection.on('NewBookingCreated', (data) => {
        playChime();
        setAlert(data);
      });

      connection.start().catch(() => {});
    } catch {
      // Hub connection optional
    }

    return () => {
      if (connection) connection.stop();
    };
  }, [user]);

  if (!alert) return null;

  return (
    <div
      style={{
        position: 'fixed',
        bottom: '24px',
        right: '24px',
        zIndex: 9999,
        background: '#1f4d45',
        color: '#fff',
        padding: '1rem 1.25rem',
        borderRadius: '10px',
        boxShadow: '0 8px 30px rgba(0,0,0,0.3)',
        maxWidth: '360px',
        animation: 'slideUp 0.3s ease-out',
        border: '1px solid rgba(255,255,255,0.2)',
      }}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.4rem' }}>
        <strong style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', color: '#f3d9a2', fontSize: '1.05rem' }}>
          🔔 Đơn đặt phòng mới #{alert.bookingId}!
        </strong>
        <button
          type="button"
          onClick={() => setAlert(null)}
          style={{ background: 'transparent', border: 'none', color: '#fff', fontSize: '1.2rem', cursor: 'pointer', opacity: 0.8 }}
        >
          ✕
        </button>
      </div>
      <p style={{ margin: '0.2rem 0', fontSize: '0.9rem', lineHeight: 1.4 }}>
        Khách: <strong>{alert.guestName || 'Khách đặt'}</strong> · Phòng <strong>{alert.roomNumber}</strong>
      </p>
      <p style={{ margin: '0.2rem 0', fontSize: '0.9rem', color: '#e0f0eb' }}>
        Tổng tiền: <strong>{money(alert.totalPrice)}</strong>
      </p>
      <div style={{ marginTop: '0.75rem', display: 'flex', justifyContent: 'flex-end', gap: '0.5rem' }}>
        <button
          className="btn small"
          type="button"
          style={{ background: '#c4a574', color: '#1f4d45', fontWeight: 600, border: 'none' }}
          onClick={() => {
            setAlert(null);
            navigate('/frontdesk');
          }}
        >
          Xem quầy lễ tân
        </button>
      </div>
    </div>
  );
}

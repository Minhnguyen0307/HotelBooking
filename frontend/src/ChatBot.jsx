import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api } from './api';
import { money } from './format';
import './ChatBot.css';

export default function ChatBot() {
  const [isOpen, setIsOpen] = useState(false);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [messages, setMessages] = useState([
    {
      id: 'welcome',
      role: 'assistant',
      time: formatTime(new Date()),
      text: 'Dạ xin chào quý khách! Em là **Trợ lý AI của HBS Luxury Hotel** 🏨.\n\nEm luôn sẵn sàng hỗ trợ quý khách về:\n• 🔑 Thủ tục nhận phòng (Check-in 14:00) & Trả phòng (Check-out 12:00)\n• 📝 Hướng dẫn các bước đặt phòng & Thanh toán linh hoạt\n• 📞 Hotline lễ tân & Địa chỉ 5 cơ sở tại Đà Nẵng\n• 🔍 Tìm kiếm phòng theo yêu cầu (view biển, bồn tắm, số người)\n• 🎁 Mã giảm giá và chính sách phụ thu\n\nQuý khách muốn em hỗ trợ gì trước ạ?',
      suggestedQuestions: [
        'Thủ tục nhận & trả phòng thế nào?',
        'Tìm phòng cho 2 người ở Mỹ Khê',
        'Các bước đặt phòng và thanh toán',
        'Hotline lễ tân và địa chỉ 5 cơ sở',
        'Mã giảm giá đang có là gì?'
      ],
      recommendedRooms: []
    }
  ]);

  const messagesEndRef = useRef(null);
  const inputRef = useRef(null);
  const navigate = useNavigate();

  function scrollToBottom() {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }

  useEffect(() => {
    if (isOpen) {
      scrollToBottom();
      setTimeout(() => inputRef.current?.focus(), 200);
    }
  }, [isOpen, messages, loading]);

  async function handleSend(textToSend) {
    const text = (textToSend || input).trim();
    if (!text || loading) return;

    const userMsg = {
      id: 'user-' + Date.now(),
      role: 'user',
      time: formatTime(new Date()),
      text
    };

    const nextMessages = [...messages, userMsg];
    setMessages(nextMessages);
    setInput('');
    setLoading(true);

    try {
      // Chuẩn bị lịch sử tin nhắn gần nhất
      const history = nextMessages
        .filter((m) => m.text)
        .slice(-6)
        .map((m) => ({
          role: m.role,
          content: m.text
        }));

      const res = await api('/api/chat/ask', {
        method: 'POST',
        body: {
          message: text,
          history
        }
      });

      const assistantMsg = {
        id: 'assistant-' + Date.now(),
        role: 'assistant',
        time: formatTime(new Date()),
        text: res.reply || 'Dạ quý khách cần hỗ trợ thêm thông tin gì nữa không ạ?',
        suggestedQuestions: res.suggestedQuestions || [],
        recommendedRooms: res.recommendedRooms || []
      };

      setMessages((prev) => [...prev, assistantMsg]);
    } catch (err) {
      console.error('Chat error:', err);
      const errorMsg = {
        id: 'assistant-err-' + Date.now(),
        role: 'assistant',
        time: formatTime(new Date()),
        text: 'Xin lỗi quý khách, tạm thời không thể kết nối tới máy chủ. Quý khách vui lòng liên hệ hotline lễ tân **1900 8888** để được hỗ trợ tức thì ạ!'
      };
      setMessages((prev) => [...prev, errorMsg]);
    } finally {
      setLoading(false);
    }
  }

  function onKeyDown(e) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  }

  function renderFormattedText(text) {
    if (!text) return null;
    const lines = text.split('\n');
    return lines.map((line, i) => {
      // Format **bold**
      const parts = line.split(/(\*\*.*?\*\*)/g);
      return (
        <p key={i}>
          {parts.map((part, j) => {
            if (part.startsWith('**') && part.endsWith('**')) {
              return <strong key={j}>{part.slice(2, -2)}</strong>;
            }
            if (part.startsWith('*') && part.endsWith('*')) {
              return <em key={j}>{part.slice(1, -1)}</em>;
            }
            return part;
          })}
        </p>
      );
    });
  }

  return (
    <>
      {/* Floating Action Button */}
      {!isOpen && (
        <button
          className="chatbot-fab"
          onClick={() => setIsOpen(true)}
          aria-label="Mở trợ lý AI HBS Hotel"
          type="button"
        >
          <span className="chatbot-fab-badge" />
          <div className="chatbot-fab-icon">🤖</div>
          <div className="chatbot-fab-label">
            <span className="chatbot-fab-title">AI Tư Vấn</span>
            <span className="chatbot-fab-sub">HBS Hotel 24/7</span>
          </div>
        </button>
      )}

      {/* Chat Window Panel */}
      {isOpen && (
        <div className="chatbot-window">
          {/* Header */}
          <div className="chatbot-header">
            <div className="chatbot-header-info">
              <div className="chatbot-avatar">✨</div>
              <div>
                <h3 className="chatbot-title">HBS Assistant</h3>
                <p className="chatbot-status">
                  <span className="chatbot-status-dot" /> Trực tuyến 24/7
                </p>
              </div>
            </div>
            <div className="chatbot-header-actions">
              <button
                className="chatbot-icon-btn"
                onClick={() => setIsOpen(false)}
                title="Đóng cửa sổ chat"
                type="button"
              >
                ✕
              </button>
            </div>
          </div>

          {/* Messages Container */}
          <div className="chatbot-messages">
            {messages.map((msg) => (
              <div key={msg.id} className={`chatbot-msg-row ${msg.role}`}>
                <div className="chatbot-bubble">
                  {renderFormattedText(msg.text)}

                  {/* Room recommendation cards if any */}
                  {msg.recommendedRooms && msg.recommendedRooms.length > 0 && (
                    <div className="chatbot-room-list">
                      {msg.recommendedRooms.map((room) => (
                        <Link
                          key={room.roomId}
                          to={`/rooms/${room.roomId}`}
                          className="chatbot-room-card"
                          onClick={() => setIsOpen(false)}
                        >
                          <img
                            src={room.primaryImageUrl || '/images/room_standard.png'}
                            alt={room.roomNumber}
                            className="chatbot-room-img"
                            onError={(e) => {
                              e.currentTarget.src = '/images/room_standard.png';
                            }}
                          />
                          <div className="chatbot-room-info">
                            <div>
                              <div className="chatbot-room-name">
                                Phòng {room.roomNumber} · {room.typeName}
                              </div>
                              <div className="chatbot-room-property">
                                {room.propertyName} ({room.area})
                              </div>
                            </div>
                            <div className="chatbot-room-bottom">
                              <span className="chatbot-room-price">
                                {money(room.basePrice)}/đêm
                              </span>
                              <span className="chatbot-room-btn">Xem phòng ➜</span>
                            </div>
                          </div>
                        </Link>
                      ))}
                    </div>
                  )}
                </div>

                {/* Suggestions chips below assistant response */}
                {msg.role === 'assistant' && msg.suggestedQuestions && msg.suggestedQuestions.length > 0 && (
                  <div className="chatbot-suggestions">
                    {msg.suggestedQuestions.map((q, idx) => (
                      <button
                        key={idx}
                        className="chatbot-chip"
                        type="button"
                        onClick={() => handleSend(q)}
                      >
                        {q}
                      </button>
                    ))}
                  </div>
                )}

                <span className="chatbot-time">{msg.time}</span>
              </div>
            ))}

            {/* Typing Indicator */}
            {loading && (
              <div className="chatbot-msg-row assistant">
                <div className="chatbot-typing">
                  <span className="chatbot-typing-dot" />
                  <span className="chatbot-typing-dot" />
                  <span className="chatbot-typing-dot" />
                </div>
              </div>
            )}

            <div ref={messagesEndRef} />
          </div>

          {/* Input Bar */}
          <form
            className="chatbot-input-bar"
            onSubmit={(e) => {
              e.preventDefault();
              handleSend();
            }}
          >
            <input
              ref={inputRef}
              type="text"
              className="chatbot-input"
              placeholder="Hỏi về thủ tục, giá phòng, cơ sở..."
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={onKeyDown}
              disabled={loading}
            />
            <button
              type="submit"
              className="chatbot-send-btn"
              disabled={loading || !input.trim()}
              title="Gửi câu hỏi"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <line x1="22" y1="2" x2="11" y2="13" />
                <polygon points="22 2 15 22 11 13 2 9 22 2" />
              </svg>
            </button>
          </form>
        </div>
      )}
    </>
  );
}

function formatTime(date) {
  const h = String(date.getHours()).padStart(2, '0');
  const m = String(date.getMinutes()).padStart(2, '0');
  return `${h}:${m}`;
}

document.addEventListener('DOMContentLoaded', () => {
    const chatMessages = document.getElementById('chat-messages');
    const chatInput = document.getElementById('chat-input');
    const sendButton = document.getElementById('send-button');

    let chatHistory = [];
    let sessionId = localStorage.getItem('aiChatSessionId') || null;

    const saveChatHistory = () => {
        sessionStorage.setItem('chatHistory', JSON.stringify(chatHistory));
    };

    const saveSessionId = (id) => {
        sessionId = id;
        localStorage.setItem('aiChatSessionId', id);
    };

    const addMessage = (text, sender, isTyping = false) => {
        const messageId = `msg-${Date.now()}`;

        if (!isTyping) {
            chatHistory.push({ text, sender });
            saveChatHistory();
        }

        const messageElement = document.createElement('div');
        messageElement.classList.add('message', sender);
        messageElement.id = messageId;

        let content;
        if (isTyping) {
            content = `
                <div class="message-content">
                    <div class="typing-indicator">
                        <span></span>
                        <span></span>
                        <span></span>
                    </div>
                </div>
            `;
        } else {
            // Sanitize text to prevent HTML injection
            const sanitizedText = text.replace(/</g, "&lt;").replace(/>/g, "&gt;");
            content = `<div class="message-content">${sanitizedText}</div>`;
        }

        const avatar = `<div class="message-avatar"><i class="fas ${sender === 'user' ? 'fa-user' : 'fa-robot'}"></i></div>`;

        messageElement.innerHTML = `
            ${avatar}
            ${content}
        `;

        chatMessages.appendChild(messageElement);
        chatMessages.scrollTop = chatMessages.scrollHeight;
        return messageId;
    };

    const handleUserMessage = async () => {
        const messageText = chatInput.value.trim();
        if (messageText === '') return;

        addMessage(messageText, 'user');
        chatInput.value = '';
        chatInput.disabled = true;
        sendButton.disabled = true;

        const typingId = addMessage('', 'ai', true);

        try {
            const response = await fetch('/api/AiAssistant', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('jwtToken')}`
                },
                body: JSON.stringify({
                    query: messageText,
                    sessionId: sessionId
                })
            });

            const typingElement = document.getElementById(typingId);
            if (typingElement) {
                typingElement.remove();
            }

            if (!response.ok) {
                let errorMessage = 'Sunucudan bir hata yanıtı alındı.';
                try {
                    const errorData = await response.json();
                    errorMessage = errorData.response || errorData.message || JSON.stringify(errorData);
                } catch (e) {
                    const errorText = await response.text();
                    errorMessage = errorText || `HTTP ${response.status}: ${response.statusText}`;
                }
                console.error('API Hatası:', errorMessage);
                throw new Error(errorMessage);
            }

            const result = await response.json();

            // Session ID'yi kaydet (sunucudan gelen)
            if (result.sessionId) {
                saveSessionId(result.sessionId);
            }

            addMessage(result.response, 'ai');

        } catch (error) {
            addMessage(`Hata: ${error.message}`, 'ai');
        } finally {
            chatInput.disabled = false;
            sendButton.disabled = false;
            chatInput.focus();
        }
    };

    const loadChatHistory = () => {
        const savedHistory = sessionStorage.getItem('chatHistory');
        if (savedHistory) {
            chatHistory = JSON.parse(savedHistory);
            chatMessages.innerHTML = ''; // Clear existing messages before loading
            chatHistory.forEach(msg => {
                // Directly create message elements without pushing to history again
                const messageElement = document.createElement('div');
                messageElement.classList.add('message', msg.sender);
                const sanitizedText = msg.text.replace(/</g, "&lt;").replace(/>/g, "&gt;");
                const content = `<div class="message-content">${sanitizedText}</div>`;
                const avatar = `<div class="message-avatar"><i class="fas ${msg.sender === 'user' ? 'fa-user' : 'fa-robot'}"></i></div>`;
                messageElement.innerHTML = `${avatar}${content}`;
                chatMessages.appendChild(messageElement);
            });
            chatMessages.scrollTop = chatMessages.scrollHeight;
        } else {
            // Initial welcome message only if there's no history
            setTimeout(() => {
                addMessage("Merhaba! Ben InventoryETS asistanınız. Stok, ürün veya tedarikçiler hakkında bilgi almak için bana soru sorabilirsiniz.", 'ai');
            }, 500);
        }
    };

    // Yeni konuşma başlatma butonu (opsiyonel - eklemek isterseniz)
    const clearChatButton = document.getElementById('clear-chat-button');
    if (clearChatButton) {
        clearChatButton.addEventListener('click', async () => {
            if (confirm('Konuşma geçmişini temizlemek istediğinize emin misiniz?')) {
                // Backend'deki session'ı temizle
                if (sessionId) {
                    try {
                        await fetch('/api/AiAssistant/clear-session', {
                            method: 'POST',
                            headers: {
                                'Content-Type': 'application/json',
                                'Authorization': `Bearer ${localStorage.getItem('jwtToken')}`
                            },
                            body: JSON.stringify(sessionId)
                        });
                    } catch (error) {
                        console.error('Session temizleme hatası:', error);
                    }
                }

                // Frontend'deki verileri temizle
                sessionStorage.removeItem('chatHistory');
                localStorage.removeItem('aiChatSessionId');
                chatHistory = [];
                sessionId = null;
                chatMessages.innerHTML = '';

                // Hoş geldin mesajını göster
                setTimeout(() => {
                    addMessage("Merhaba! Ben InventoryETS asistanınız. Stok, ürün veya tedarikçiler hakkında bilgi almak için bana soru sorabilirsiniz.", 'ai');
                }, 500);
            }
        });
    }

    // Session timeout kontrolü (opsiyonel - 30 dakika sonra session'ı sıfırla)
    let sessionTimeout;
    const resetSessionTimeout = () => {
        clearTimeout(sessionTimeout);
        sessionTimeout = setTimeout(() => {
            // 30 dakika aktivite yoksa session'ı temizle
            localStorage.removeItem('aiChatSessionId');
            sessionId = null;
            console.log('Session timeout - yeni session oluşturulacak');
        }, 30 * 60 * 1000); // 30 dakika
    };

    sendButton.addEventListener('click', () => {
        handleUserMessage();
        resetSessionTimeout();
    });

    chatInput.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') {
            handleUserMessage();
            resetSessionTimeout();
        }
    });

    // Sayfa yüklendiğinde session timeout'u başlat
    resetSessionTimeout();

    loadChatHistory();
});
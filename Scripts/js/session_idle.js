var idleTime = 0;

function resetIdleTimer() {
    idleTime = 0;
}

function timerIncrement() {
    idleTime = idleTime + 1;
    if (idleTime > 19) { // 5 minutes (20 * 15s)
        alert('Session Timed out!');
        window.location.href = '/Account/Logout';
    }
}

function initIdleTimer() {
    // Increment the idle time counter every 15 seconds.
    setInterval(timerIncrement, 15000);

    // Zero the idle timer on user activity
    window.addEventListener('mousemove', resetIdleTimer, false);
    window.addEventListener('keypress', resetIdleTimer, false);
    window.addEventListener('scroll', resetIdleTimer, false);
    window.addEventListener('touchstart', resetIdleTimer, false);
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initIdleTimer);
} else {
    initIdleTimer();
}


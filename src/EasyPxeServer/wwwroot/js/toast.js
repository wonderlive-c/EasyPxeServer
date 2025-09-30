function showToast(message, type = 'success', duration = 3000) {
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.textContent = message;
    
    // 样式设置
    toast.style.position = 'fixed';
    toast.style.bottom = '20px';
    toast.style.right = '20px';
    toast.style.padding = '10px 20px';
    toast.style.borderRadius = '4px';
    toast.style.color = '#fff';
    toast.style.backgroundColor = type === 'error' ? '#dc3545' : '#28a745';
    toast.style.boxShadow = '0 2px 10px rgba(0,0,0,0.1)';
    toast.style.opacity = '0';
    toast.style.transition = 'opacity 0.3s ease-in-out';
    
    document.body.appendChild(toast);
    
    // 淡入效果
    setTimeout(() => {
        toast.style.opacity = '1';
    }, 10);
    
    // 自动消失
    setTimeout(() => {
        toast.style.opacity = '0';
        setTimeout(() => {
            document.body.removeChild(toast);
        }, 300);
    }, duration);
}

// 暴露到全局
window.showToast = showToast;
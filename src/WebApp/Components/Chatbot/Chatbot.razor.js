export function scrollToEnd(element) {
    element.scrollTo({ top: element.scrollHeight, behavior: 'smooth' });
}

export function submitOnEnter(element) {
    element.addEventListener('keydown', event => {
        if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault();
            event.target.dispatchEvent(new Event('input', { bubbles: true }));
            event.target.dispatchEvent(new Event('change', { bubbles: true }));
            
            setTimeout(() => {
                const form = event.target.closest('form');
                if (form) {
                    form.dispatchEvent(new Event('submit'));
                }
            }, 10);
        }
    });
}


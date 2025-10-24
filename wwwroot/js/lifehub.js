// Funcionalidades específicas de LifeHub
document.addEventListener('DOMContentLoaded', function() {
    // Toggle del navbar en móviles
    const navbarToggler = document.getElementById('navbarToggler');
    const navbarNav = document.getElementById('navbarNav');
    
    if (navbarToggler && navbarNav) {
        navbarToggler.addEventListener('click', function() {
            this.classList.toggle('active');
            navbarNav.classList.toggle('show');
        });
    }
    
    // Cerrar el menú al hacer clic en un enlace (solo móviles)
    const navLinks = document.querySelectorAll('.lh-nav-link');
    navLinks.forEach(link => {
        link.addEventListener('click', function() {
            if (window.innerWidth < 992) {
                navbarToggler.classList.remove('active');
                navbarNav.classList.remove('show');
            }
        });
    });
    
    // Smooth scroll para los enlaces internos
    function smoothScroll(target) {
        const element = document.querySelector(target);
        if (element) {
            const offsetTop = element.offsetTop - 80; // Ajuste para el navbar fijo
            window.scrollTo({
                top: offsetTop,
                behavior: 'smooth'
            });
        }
    }
    
    // Añadir event listeners a los enlaces de navegación
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function(e) {
            const href = this.getAttribute('href');
            if (href !== '#' && href.startsWith('#')) {
                e.preventDefault();
                smoothScroll(href);
            }
        });
    });
    
    // Timeline interactivo
    const defaultImage = 'https://images.unsplash.com/photo-1506784983877-45594efa4cbe?ixlib=rb-4.0.3&auto=format&fit=crop&w=500&q=80';
    const timelineImage = document.getElementById('timeline-image');
    
    if (timelineImage) {
        const collapseElements = document.querySelectorAll('.collapse');
        
        collapseElements.forEach(collapse => {
            collapse.addEventListener('show.bs.collapse', function() {
                collapseElements.forEach(otherCollapse => {
                    if (otherCollapse !== collapse && otherCollapse.classList.contains('show')) {
                        bootstrap.Collapse.getInstance(otherCollapse).hide();
                    }
                });
                
                const header = this.closest('.timeline-content').querySelector('.timeline-header');
                const imageUrl = header.getAttribute('data-image');
                
                timelineImage.style.opacity = 0;
                setTimeout(() => {
                    timelineImage.src = imageUrl;
                    timelineImage.style.opacity = 1;
                }, 300);
            });
            
            collapse.addEventListener('hide.bs.collapse', function() {
                const openCollapses = document.querySelectorAll('.collapse.show');
                if (openCollapses.length === 1) {
                    timelineImage.style.opacity = 0;
                    setTimeout(() => {
                        timelineImage.src = defaultImage;
                        timelineImage.style.opacity = 1;
                    }, 300);
                }
            });
        });
    }
    
    // Efecto de aparición para timeline
    const timelineItems = document.querySelectorAll('.timeline-item');
    
    function checkScroll() {
        timelineItems.forEach(item => {
            const position = item.getBoundingClientRect().top;
            const screenPosition = window.innerHeight / 1.3;
            
            if (position < screenPosition) {
                item.style.opacity = 1;
                item.style.transform = 'translateY(0)';
            }
        });
    }
    
    timelineItems.forEach(item => {
        item.style.opacity = 0;
        item.style.transform = 'translateY(20px)';
        item.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
    });
    
    window.addEventListener('load', checkScroll);
    window.addEventListener('scroll', checkScroll);
    checkScroll();
    
    // Pricing cards hover effect
    const pricingCards = document.querySelectorAll('.pricing-card');
    
    pricingCards.forEach(card => {
        card.addEventListener('mouseenter', function() {
            this.style.transform = 'translateY(-10px)';
        });
        
        card.addEventListener('mouseleave', function() {
            this.style.transform = 'translateY(0)';
        });
    });
    
    // Alinear botones de precios
    function alignPricingButtons() {
        const basicButton = document.querySelector('.pricing-card:not(.premium) .pricing-button-container');
        const premiumButton = document.querySelector('.pricing-card.premium .pricing-button-container');
        
        if (basicButton && premiumButton) {
            const maxHeight = Math.max(basicButton.offsetHeight, premiumButton.offsetHeight);
            basicButton.style.minHeight = maxHeight + 'px';
            premiumButton.style.minHeight = maxHeight + 'px';
        }
    }
    
    alignPricingButtons();
    window.addEventListener('resize', alignPricingButtons);
    
    // Hacer que el número de teléfono sea clickeable
    const phoneText = document.querySelector('.info-text');
    if (phoneText) {
        phoneText.style.cursor = 'pointer';
        phoneText.title = 'Llamar al soporte';
        phoneText.addEventListener('click', function() {
            window.location.href = 'tel:16519';
        });
    }

    
});

// Funcionalidad para los botones del Hero Section
document.addEventListener('DOMContentLoaded', function() {
    console.log('LifeHub JavaScript cargado - Botones mejorados');
    
    // Botón "Iniciar Ahora" - Redirige a la página de registro
    const iniciarAhoraBtn = document.getElementById('iniciarAhoraBtn');
    if (iniciarAhoraBtn) {
        console.log('Botón "Iniciar Ahora" encontrado');
        
        iniciarAhoraBtn.addEventListener('click', function() {
            console.log('Redirigiendo a página de registro');
            // Redirigir a la página de registro (usando la misma URL que el navbar)
            window.location.href = '/Account/Register';
        });
        
        // Efectos visuales
        iniciarAhoraBtn.addEventListener('mouseenter', function() {
            this.style.transform = 'translateY(-3px)';
            this.style.boxShadow = '0 8px 15px rgba(0, 0, 0, 0.3)';
        });
        
        iniciarAhoraBtn.addEventListener('mouseleave', function() {
            this.style.transform = '';
            this.style.boxShadow = '0 4px 8px rgba(0, 0, 0, 0.2)';
        });
        
        iniciarAhoraBtn.addEventListener('mousedown', function() {
            this.style.transform = 'translateY(1px)';
        });
        
        iniciarAhoraBtn.addEventListener('mouseup', function() {
            this.style.transform = 'translateY(-3px)';
        });
    }
    
    // Botón "Ver Planes" - Scroll suave a la sección de planes
    const verPlanesBtn = document.getElementById('verPlanesBtn');
    if (verPlanesBtn) {
        console.log('Botón "Ver Planes" encontrado');
        
        verPlanesBtn.addEventListener('click', function() {
            console.log('Desplazándose a la sección de planes');
            
            const planesSection = document.getElementById('planes');
            if (planesSection) {
                const offsetTop = planesSection.offsetTop - 80; // Ajuste para navbar fijo
                window.scrollTo({
                    top: offsetTop,
                    behavior: 'smooth'
                });
            } else {
                console.error('Sección de planes no encontrada');
                // Fallback: usar hash en la URL
                window.location.hash = 'planes';
            }
        });
        
        // Efectos visuales
        verPlanesBtn.addEventListener('mouseenter', function() {
            this.style.transform = 'translateY(-3px)';
            this.style.boxShadow = '0 8px 15px rgba(255, 255, 255, 0.3)';
        });
        
        verPlanesBtn.addEventListener('mouseleave', function() {
            this.style.transform = '';
            this.style.boxShadow = '0 4px 8px rgba(0, 0, 0, 0.2)';
        });
    }
    
    // Enlace de soporte telefónico (funcionalidad nativa)
    const soporteLink = document.getElementById('soporteLink');
    if (soporteLink) {
        console.log('Enlace de soporte encontrado');
        // No necesita JavaScript adicional, funciona nativamente
    }
    
    // Botones de redes sociales
    const socialButtons = [
        { id: 'facebookBtn', url: 'https://facebook.com/lifehub' },
        { id: 'twitterBtn', url: 'https://twitter.com/lifehub' },
        { id: 'instagramBtn', url: 'https://instagram.com/lifehub' },
        { id: 'linkedinBtn', url: 'https://linkedin.com/company/lifehub' }
    ];
    
    socialButtons.forEach(social => {
        const button = document.getElementById(social.id);
        if (button) {
            button.addEventListener('click', function() {
                console.log('Redirigiendo a ' + social.url);
                window.open(social.url, '_blank');
            });
            
            // Efectos visuales para redes sociales
            button.addEventListener('mouseenter', function() {
                this.style.transform = 'translateY(-3px) scale(1.1)';
                this.style.background = 'white';
                this.style.color = 'var(--primary)';
            });
            
            button.addEventListener('mouseleave', function() {
                this.style.transform = '';
                this.style.background = 'rgba(255, 255, 255, 0.1)';
                this.style.color = 'white';
            });
        }
    });
    
    console.log('Configuración de botones completada');
});

// Función de scroll suave para todos los enlaces internos
function initSmoothScroll() {
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                const offsetTop = target.offsetTop - 80;
                window.scrollTo({
                    top: offsetTop,
                    behavior: 'smooth'
                });
            }
        });
    });
}

// Inicializar scroll suave
document.addEventListener('DOMContentLoaded', initSmoothScroll);

// Polyfill para smooth scroll en navegadores más antiguos
if (!('scrollBehavior' in document.documentElement.style)) {
    import('scroll-behavior-polyfill').then(module => {
        module.default();
    });
}

// Función de diagnóstico para el navbar
function debugNavbar() {
    console.log('=== DIAGNÓSTICO NAVBAR ===');
    
    const navbar = document.querySelector('.lh-navbar');
    if (navbar) {
        const style = window.getComputedStyle(navbar);
        console.log('Navbar:', {
            position: style.position,
            zIndex: style.zIndex,
            top: style.top,
            width: style.width,
            height: style.height
        });
    }
    
    // Verificar superposición
    const heroSection = document.querySelector('.hero-section');
    if (heroSection) {
        const rect = heroSection.getBoundingClientRect();
        console.log('Hero Section position:', rect.top);
        
        // Verificar si el navbar está cubriendo el contenido
        if (rect.top < 80) {
            console.warn('¡El navbar está cubriendo el contenido!');
            // Solución de emergencia
            document.body.style.paddingTop = '80px';
            heroSection.style.marginTop = '80px';
        }
    }
}

// Forzar estilos importantes
function forceImportantStyles() {
    // Forzar navbar en la parte superior
    const navbar = document.querySelector('.lh-navbar');
    if (navbar) {
        navbar.style.position = 'fixed !important';
        navbar.style.top = '0 !important';
        navbar.style.left = '0 !important';
        navbar.style.width = '100% !important';
        navbar.style.zIndex = '9999 !important';
    }
    
    // Forzar padding en el body
    document.body.style.paddingTop = '80px !important';
    
    // Asegurar que el contenido esté debajo
    const mainContent = document.querySelector('main') || document.body;
    mainContent.style.position = 'relative';
    mainContent.style.zIndex = '1';
}

// Ejecutar al cargar
document.addEventListener('DOMContentLoaded', function() {
    console.log('Aplicando solución definitiva para navbar...');
    
    debugNavbar();
    forceImportantStyles();
    
    // Revisar cada segundo por si hay cambios (para SPA)
    setInterval(forceImportantStyles, 1000);
});

// También ejecutar cuando se redimensiona la ventana
window.addEventListener('resize', forceImportantStyles);


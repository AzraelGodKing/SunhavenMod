/* ===================================================================
   Sun Haven Mod Hub - Shared JavaScript
   =================================================================== */

// Dark mode: apply stored theme immediately to prevent flash
(function() {
    var stored = localStorage.getItem('sh-mod-theme');
    if (stored) {
        document.documentElement.setAttribute('data-theme', stored);
    }
})();

document.addEventListener('DOMContentLoaded', function() {

    // -----------------------------------------------------------------
    // 1. BACK-TO-TOP BUTTON
    // -----------------------------------------------------------------
    var topBtn = document.createElement('button');
    topBtn.className = 'back-to-top';
    topBtn.setAttribute('aria-label', 'Back to top');
    topBtn.innerHTML = '\u2191';
    topBtn.title = 'Back to top';
    document.body.appendChild(topBtn);

    window.addEventListener('scroll', function() {
        if (window.scrollY > 300) {
            topBtn.classList.add('visible');
        } else {
            topBtn.classList.remove('visible');
        }
    });

    topBtn.addEventListener('click', function() {
        var instant = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        window.scrollTo({ top: 0, behavior: instant ? 'auto' : 'smooth' });
    });

    // -----------------------------------------------------------------
    // 2. DARK MODE TOGGLE
    // -----------------------------------------------------------------
    var toggle = document.createElement('button');
    toggle.className = 'theme-toggle';
    toggle.setAttribute('aria-label', 'Toggle dark mode');
    var currentTheme = document.documentElement.getAttribute('data-theme');
    toggle.innerHTML = currentTheme === 'dark' ? '\u2600\uFE0F' : '\uD83C\uDF19';
    document.body.appendChild(toggle);

    toggle.addEventListener('click', function() {
        var current = document.documentElement.getAttribute('data-theme');
        var next = current === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-theme', next);
        localStorage.setItem('sh-mod-theme', next);
        toggle.innerHTML = next === 'dark' ? '\u2600\uFE0F' : '\uD83C\uDF19';
    });

    // -----------------------------------------------------------------
    // 3. COPY-TO-CLIPBOARD FOR CONFIG PATHS
    // -----------------------------------------------------------------
    var configCodes = document.querySelectorAll('.config-note code');
    configCodes.forEach(function(codeEl) {
        var wrapper = document.createElement('span');
        wrapper.className = 'copy-wrapper';

        var copyBtn = document.createElement('button');
        copyBtn.className = 'copy-btn';
        copyBtn.innerHTML = '\uD83D\uDCCB';
        copyBtn.title = 'Copy to clipboard';

        codeEl.parentNode.insertBefore(wrapper, codeEl);
        wrapper.appendChild(codeEl);
        wrapper.appendChild(copyBtn);

        copyBtn.addEventListener('click', function() {
            var text = codeEl.textContent;
            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(text).then(onCopied);
            } else {
                // Fallback for file:// or older browsers
                var ta = document.createElement('textarea');
                ta.value = text;
                ta.style.position = 'fixed';
                ta.style.opacity = '0';
                document.body.appendChild(ta);
                ta.select();
                document.execCommand('copy');
                document.body.removeChild(ta);
                onCopied();
            }

            function onCopied() {
                copyBtn.innerHTML = '\u2705';
                copyBtn.classList.add('copied');
                setTimeout(function() {
                    copyBtn.innerHTML = '\uD83D\uDCCB';
                    copyBtn.classList.remove('copied');
                }, 2000);
            }
        });
    });

    // -----------------------------------------------------------------
    // 4. SEARCH / FILTER ON INDEX PAGE
    // -----------------------------------------------------------------
    var searchInput = document.getElementById('modSearch');
    if (searchInput) {
        var cards = document.querySelectorAll('.mod-card');
        var tagButtons = document.querySelectorAll('.filter-tag');
        var activeTag = 'all';
        var emptyMsg = document.getElementById('modSearchEmpty');
        var resetBtn = emptyMsg ? emptyMsg.querySelector('.mod-search-reset') : null;

        function syncHubURL() {
            if (!window.history || !window.history.replaceState) return;
            var u = new URL(window.location.href);
            var rawQ = searchInput.value.trim();
            if (rawQ) u.searchParams.set('q', rawQ); else u.searchParams.delete('q');
            if (activeTag && activeTag !== 'all') u.searchParams.set('tag', activeTag);
            else u.searchParams.delete('tag');
            var qs = u.searchParams.toString();
            var search = qs ? '?' + qs : '';
            history.replaceState(null, '', u.pathname + search + (window.location.hash || ''));
        }

        function filterCards() {
            var query = searchInput.value.toLowerCase().trim();
            var visibleCount = 0;
            cards.forEach(function(card) {
                var name = (card.getAttribute('data-name') || '').toLowerCase();
                var desc = card.querySelector('.mod-description');
                var descText = desc ? desc.textContent.toLowerCase() : '';
                var features = card.querySelectorAll('.mod-feature');
                var featureText = Array.from(features).map(function(f) {
                    return f.textContent.toLowerCase();
                }).join(' ');
                var tags = (card.getAttribute('data-tags') || '').split(',');

                var matchesSearch = !query || name.indexOf(query) !== -1 ||
                    descText.indexOf(query) !== -1 || featureText.indexOf(query) !== -1;
                var matchesTag = activeTag === 'all' || tags.indexOf(activeTag) !== -1;

                var show = matchesSearch && matchesTag;
                card.style.display = show ? '' : 'none';
                if (show) visibleCount++;
            });
            if (emptyMsg) {
                if (visibleCount === 0) {
                    emptyMsg.hidden = false;
                } else {
                    emptyMsg.hidden = true;
                }
            }
            syncHubURL();
        }

        function applyHubParamsFromURL() {
            var u = new URL(window.location.href);
            var qParam = u.searchParams.get('q');
            var tagParam = u.searchParams.get('tag');
            if (qParam !== null) searchInput.value = qParam;
            if (tagParam) {
                var matchBtn = null;
                tagButtons.forEach(function(b) {
                    if (b.getAttribute('data-tag') === tagParam) matchBtn = b;
                });
                if (matchBtn) {
                    tagButtons.forEach(function(b) { b.classList.remove('active'); });
                    matchBtn.classList.add('active');
                    activeTag = tagParam;
                }
            }
            filterCards();
        }

        searchInput.addEventListener('input', filterCards);

        tagButtons.forEach(function(btn) {
            btn.addEventListener('click', function() {
                tagButtons.forEach(function(b) { b.classList.remove('active'); });
                btn.classList.add('active');
                activeTag = btn.getAttribute('data-tag');
                filterCards();
            });
        });

        if (resetBtn) {
            resetBtn.addEventListener('click', function() {
                searchInput.value = '';
                activeTag = 'all';
                tagButtons.forEach(function(b) { b.classList.remove('active'); });
                var allBtn = document.querySelector('.filter-tag[data-tag="all"]');
                if (allBtn) allBtn.classList.add('active');
                filterCards();
                searchInput.focus();
            });
        }

        applyHubParamsFromURL();

        document.addEventListener('keydown', function(e) {
            if (e.defaultPrevented) return;
            var tag = e.target && e.target.tagName;
            if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return;
            if (e.target && e.target.isContentEditable) return;
            if (e.ctrlKey || e.metaKey || e.altKey) return;
            if (e.key === '/' || e.key === 's') {
                e.preventDefault();
                searchInput.focus();
            }
        });
    }

    // -----------------------------------------------------------------
    // Hub jump nav (index — select on narrow screens)
    // -----------------------------------------------------------------
    var hubJump = document.getElementById('hubJumpSelect');
    if (hubJump) {
        hubJump.addEventListener('change', function() {
            var v = hubJump.value;
            if (!v) return;
            location.hash = v;
            hubJump.selectedIndex = 0;
        });
    }

    // -----------------------------------------------------------------
    // 5. DYNAMIC VERSION BADGES (from versions.json)
    // -----------------------------------------------------------------
    (function() {
        // Map mod-card data-name to versions.json key
        var nameToKey = {
            'The Vault': 'com.azraelgodking.thevault',
            "Haven's Birthright": 'com.azraelgodking.havensbirthright',
            'Faster Races': 'com.azraelgodking.fasterraces',
            'Trinket Fortune': 'com.azraelgodking.trinketfortune',
            'S.M.U.T.': 'com.azraelgodking.sunhavenmuseumutilitytracker',
            'Sun Haven Todo': 'com.azraelgodking.sunhaventodo',
            "A Squirrel's Birthday Reminder": 'com.azraelgodking.squirrelsbirthdayreminder',
            "Senpai's Chest": 'com.azraelgodking.senpaischest',
            "Haven's Almanac": 'com.azraelgodking.havensalmanac',
            'HavenDevTools': 'com.azraelgodking.havendevtools'
        };

        // Determine base path for versions.json
        var scripts = document.querySelectorAll('script[src*="shared.js"]');
        var basePath = '';
        if (scripts.length) {
            var src = scripts[0].getAttribute('src');
            basePath = src.replace('shared.js', '');
        }

        fetch(basePath + 'versions.json')
            .then(function(r) { return r.json(); })
            .then(function(data) {
                // Update index page mod cards
                var cards = document.querySelectorAll('.mod-card[data-name]');
                cards.forEach(function(card) {
                    var name = card.getAttribute('data-name');
                    var key = nameToKey[name];
                    if (key && data[key]) {
                        var ver = data[key].version;
                        var badge = card.querySelector('.mod-version');
                        if (badge) badge.textContent = 'v' + ver;
                        // Update update-badge if present
                        var updateBadge = card.querySelector('.update-badge');
                        if (updateBadge && updateBadge.textContent.indexOf('Updated') !== -1) {
                            updateBadge.textContent = 'Updated v' + ver;
                        }
                    }
                });

                // Update subpage version badge
                var pageBadge = document.querySelector('.hero .version-badge');
                if (pageBadge) {
                    // Find which mod this page is for by checking title or nav
                    var navCurrent = document.querySelector('.nav-current');
                    var pageName = navCurrent ? navCurrent.textContent.trim() : '';
                    // Map nav names to keys
                    var navToKey = {
                        'The Vault': 'com.azraelgodking.thevault',
                        "Haven's Birthright": 'com.azraelgodking.havensbirthright',
                        'Faster Races': 'com.azraelgodking.fasterraces',
                        'Trinket Fortune': 'com.azraelgodking.trinketfortune',
                        'S.M.U.T.': 'com.azraelgodking.sunhavenmuseumutilitytracker',
                        'Todo List': 'com.azraelgodking.sunhaventodo',
                        'Sun Haven Todo': 'com.azraelgodking.sunhaventodo',
                        'Birthday Reminder': 'com.azraelgodking.squirrelsbirthdayreminder',
                        "Senpai's Chest": 'com.azraelgodking.senpaischest',
                        "Haven's Almanac": 'com.azraelgodking.havensalmanac',
                        'HavenDevTools': 'com.azraelgodking.havendevtools'
                    };
                    var pageKey = navToKey[pageName];
                    if (pageKey && data[pageKey]) {
                        pageBadge.textContent = 'v' + data[pageKey].version;
                    }
                }
            })
            .catch(function() { /* versions.json not available, keep static values */ });
    })();

    // -----------------------------------------------------------------
    // 5b. MOD DOWNLOAD NAV (Thunderstore, Nexus, Direct Download)
    // -----------------------------------------------------------------
    (function() {
        var nav = document.querySelector('.download-nav[data-mod-key]');
        if (!nav) return;

        var modKey = nav.getAttribute('data-mod-key');
        var baseUrl = '';
        var scriptEl = document.querySelector('script[src*="shared.js"]');
        if (scriptEl) {
            var src = scriptEl.getAttribute('src');
            baseUrl = new URL(src, document.baseURI).href.replace(/[^/]+$/, '');
        }

        fetch(baseUrl + 'versions.json')
            .then(function(r) { return r.json(); })
            .then(function(data) {
                var m = data[modKey];
                if (!m) return;

                var links = [];
                var ghBase = 'https://github.com/AzraelGodKing/SunhavenMod/releases';

                if (m.thunderstore) {
                    links.push({
                        href: m.thunderstore,
                        cls: 'download-thunderstore',
                        text: 'Thunderstore',
                        icon: '\u26A1',
                        external: true
                    });
                }
                if (m.nexus) {
                    links.push({
                        href: m.nexus,
                        cls: 'download-nexus',
                        text: 'Nexus Mods',
                        icon: '\u{1F4E6}',
                        external: true
                    });
                }
                if (m.thunderstoreName && m.version) {
                    var tag = m.thunderstoreName + '-v' + m.version;
                    var zip = m.thunderstoreName + '-' + m.version + '.zip';
                    links.push({
                        href: ghBase + '/download/' + tag + '/' + zip,
                        cls: 'download-direct',
                        text: 'Download Latest',
                        icon: '\u2B07',
                        external: true
                    });
                }

                if (links.length === 0) {
                    nav.style.display = 'none';
                    return;
                }

                var html = '<span class="download-nav-label">Get this mod</span><div class="download-nav-links">';
                links.forEach(function(l) {
                    html += '<a href="' + l.href + '" class="' + l.cls + '"' +
                        (l.external ? ' target="_blank" rel="noopener noreferrer"' : '') + '>' +
                        '<span aria-hidden="true">' + l.icon + '</span>' + l.text + '</a>';
                });
                html += '</div>';
                nav.innerHTML = html;
            })
            .catch(function() {
                nav.style.display = 'none';
            });
    })();

    // -----------------------------------------------------------------
    // 6. AUTO-GENERATED TABLE OF CONTENTS (long pages)
    // -----------------------------------------------------------------
    (function() {
        var tocContainer = document.querySelector('.toc-sidebar');
        if (!tocContainer) return;

        var headings = document.querySelectorAll('.container h2.section-title');
        if (headings.length < 3) { tocContainer.style.display = 'none'; return; }

        var tocList = document.createElement('ul');
        tocList.className = 'toc-list';
        headings.forEach(function(h, i) {
            var id = h.id || 'section-' + i;
            h.id = id;
            var li = document.createElement('li');
            var a = document.createElement('a');
            a.href = '#' + id;
            a.textContent = h.textContent;
            a.className = 'toc-link';
            li.appendChild(a);
            tocList.appendChild(li);
        });
        tocContainer.appendChild(tocList);

        // Collapsed TOC on narrow viewports (sidebar hidden via CSS)
        var mainContainer = document.querySelector('.container');
        if (mainContainer && tocList.querySelector('li')) {
            var mobileToc = document.createElement('details');
            mobileToc.className = 'toc-mobile';
            mobileToc.open = false;
            var sum = document.createElement('summary');
            sum.className = 'toc-mobile-summary';
            sum.textContent = 'On this page';
            mobileToc.appendChild(sum);
            mobileToc.appendChild(tocList.cloneNode(true));
            mainContainer.insertBefore(mobileToc, mainContainer.firstChild);
        }

        // Scroll-spy for TOC
        var tocLinks = tocContainer.querySelectorAll('.toc-link');
        window.addEventListener('scroll', function() {
            var scrollPos = window.scrollY + 160;
            var activeLink = tocLinks[0];
            headings.forEach(function(h, i) {
                if (h.offsetTop <= scrollPos) activeLink = tocLinks[i];
            });
            tocLinks.forEach(function(l) { l.classList.remove('active'); });
            if (activeLink) activeLink.classList.add('active');
        });
    })();

    // -----------------------------------------------------------------
    // 7. PAGE TRANSITION ANIMATIONS
    // -----------------------------------------------------------------
    (function() {
        var animateEls = document.querySelectorAll(
            '.mod-card, .feature-card, .install-step, .step, .faq-item, ' +
            '.pack-card, .keybind-card, .related-card, .race-card, ' +
            '.currency-category, .rule-card, .tool-category, .comparison-table, ' +
            '.mod-status-badge'
        );
        if (!animateEls.length) return;

        var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        if (reduceMotion) {
            animateEls.forEach(function(el) {
                el.classList.add('animate-in', 'animated');
            });
            return;
        }

        animateEls.forEach(function(el) { el.classList.add('animate-in'); });

        var observer = new IntersectionObserver(function(entries) {
            entries.forEach(function(entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('animated');
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.1, rootMargin: '0px 0px -40px 0px' });

        animateEls.forEach(function(el, i) {
            el.style.transitionDelay = (i % 6) * 0.08 + 's';
            observer.observe(el);
        });
    })();

    // -----------------------------------------------------------------
    // 8. ANNOUNCEMENT BANNER (Index Page)
    // -----------------------------------------------------------------
    (function() {
        var banner = document.getElementById('announcementBanner');
        if (!banner) return;

        var key = banner.getAttribute('data-dismiss-key') || 'announce-default';
        if (localStorage.getItem('sh-dismiss-' + key)) {
            banner.remove();
            return;
        }

        banner.classList.add('visible');

        var dismissBtn = banner.querySelector('.announcement-dismiss');
        if (dismissBtn) {
            dismissBtn.addEventListener('click', function() {
                banner.classList.remove('visible');
                banner.classList.add('hiding');
                setTimeout(function() { banner.remove(); }, 400);
                localStorage.setItem('sh-dismiss-' + key, '1');
            });
        }
    })();

    // -----------------------------------------------------------------
    // 9. REPORT A BUG FLOATING BUTTON
    // -----------------------------------------------------------------
    (function() {
        var bugBtn = document.createElement('a');
        bugBtn.href = 'https://discord.gg/Vwh2y7qMXv';
        bugBtn.target = '_blank';
        bugBtn.rel = 'noopener noreferrer';
        bugBtn.className = 'bug-report-btn';
        bugBtn.setAttribute('aria-label', 'Report a bug on Discord');
        bugBtn.title = 'Report a bug on Discord';
        bugBtn.innerHTML = '<span class="bug-icon">&#x1F41B;</span><span class="bug-label">Report Bug</span>';
        document.body.appendChild(bugBtn);
    })();

    // -----------------------------------------------------------------
    // 10. SCROLL-SPY FOR RACIALBONUS NAV PILLS
    // -----------------------------------------------------------------
    var pills = document.querySelectorAll('.nav-pill');
    if (pills.length) {
        var sections = [];
        pills.forEach(function(pill) {
            var href = pill.getAttribute('href');
            if (href && href.charAt(0) === '#') {
                var section = document.getElementById(href.substring(1));
                if (section) sections.push({ pill: pill, section: section });
            }
        });

        if (sections.length) {
            function updateActivePill() {
                var scrollPos = window.scrollY + 150;
                var active = sections[0];
                sections.forEach(function(s) {
                    if (s.section.offsetTop <= scrollPos) active = s;
                });
                pills.forEach(function(p) { p.classList.remove('active'); });
                if (active) active.pill.classList.add('active');
            }
            window.addEventListener('scroll', updateActivePill);
            updateActivePill();
        }
    }

});

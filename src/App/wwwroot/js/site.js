// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(() => {
    const mobileSidebarQuery = '(max-width: 767.98px)';
    const brlFormatter = new Intl.NumberFormat('pt-BR', {
        style: 'currency',
        currency: 'BRL'
    });

    const parseBrazilianNumber = (value) => {
        if (!value) {
            return Number.NaN;
        }

        const normalized = value
            .toString()
            .replace(/[^\d,.-]/g, '')
            .replace(/\./g, '')
            .replace(',', '.');

        const parsed = Number.parseFloat(normalized);
        return Number.isFinite(parsed) ? parsed : Number.NaN;
    };

    const normalizeBrazilianDecimal = (value) => {
        const parsed = parseBrazilianNumber(value);

        if (!Number.isFinite(parsed)) {
            return '';
        }

        return parsed.toString().replace('.', ',');
    };

    const normalizeBrazilianCurrency = (value) => {
        const parsed = parseBrazilianNumber(value);

        if (!Number.isFinite(parsed)) {
            return '';
        }

        return parsed.toFixed(2).replace('.', ',');
    };

    const formatCurrencyInput = (input) => {
        const digits = input.value.replace(/\D/g, '');

        if (!digits) {
            input.value = '';
            return;
        }

        input.value = brlFormatter.format(Number.parseInt(digits, 10) / 100);
    };

    const normalizeNumericInputs = (form) => {
        form.querySelectorAll('[data-currency-input]').forEach((input) => {
            input.value = normalizeBrazilianCurrency(input.value);
        });

        form.querySelectorAll('[data-decimal-input]').forEach((input) => {
            input.value = normalizeBrazilianDecimal(input.value);
        });
    };

    const closeMobileSidebarSubmenus = () => {
        if (!window.matchMedia(mobileSidebarQuery).matches) {
            return;
        }

        document.querySelectorAll('#accordionSidebar .collapse.show').forEach((submenu) => {
            submenu.classList.remove('show');
        });

        document.querySelectorAll('#accordionSidebar [data-toggle="collapse"]').forEach((toggle) => {
            toggle.classList.add('collapsed');
            toggle.setAttribute('aria-expanded', 'false');
        });
    };

    const configureBrazilianDecimalValidation = () => {
        if (!window.jQuery || !window.jQuery.validator) {
            return;
        }

        const validator = window.jQuery.validator;

        validator.methods.number = function (value, element) {
            return this.optional(element) || Number.isFinite(parseBrazilianNumber(value));
        };

        validator.methods.min = function (value, element, param) {
            const parsed = parseBrazilianNumber(value);
            return this.optional(element) || (Number.isFinite(parsed) && parsed >= param);
        };

        validator.methods.max = function (value, element, param) {
            const parsed = parseBrazilianNumber(value);
            return this.optional(element) || (Number.isFinite(parsed) && parsed <= param);
        };

        validator.methods.range = function (value, element, param) {
            const parsed = parseBrazilianNumber(value);
            return this.optional(element) || (Number.isFinite(parsed) && parsed >= param[0] && parsed <= param[1]);
        };

        validator.methods.step = function (value, element) {
            return this.optional(element) || Number.isFinite(parseBrazilianNumber(value));
        };
    };

    document.addEventListener('input', (event) => {
        const input = event.target.closest?.('[data-currency-input]');

        if (input) {
            formatCurrencyInput(input);
        }
    }, true);

    document.addEventListener('submit', (event) => {
        normalizeNumericInputs(event.target);
    }, true);

    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('[data-currency-input]').forEach(formatCurrencyInput);
        closeMobileSidebarSubmenus();
        configureBrazilianDecimalValidation();
    });
})();

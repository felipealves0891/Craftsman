(() => {
    const fragmentId = "production-calendar-fragment";
    const initialFragment = document.getElementById(fragmentId);

    if (!initialFragment || initialFragment.dataset.autoUpdate !== "true") {
        return;
    }

    const monthUrl = initialFragment.dataset.monthUrl;
    const calendarUrl = initialFragment.dataset.calendarUrl;
    const pollIntervalMilliseconds = 30_000;
    let requestInProgress = false;
    let pollTimer;

    async function checkForMonthChange() {
        if (requestInProgress || document.visibilityState !== "visible") {
            return;
        }

        const plannedDateInput = document.querySelector("input[name='plannedDate']");
        if (plannedDateInput?.value) {
            return;
        }

        requestInProgress = true;

        try {
            const monthResponse = await fetch(monthUrl, { cache: "no-store" });
            if (!monthResponse.ok) {
                return;
            }

            const month = await monthResponse.json();
            const currentMonth = `${month.year}-${String(month.month).padStart(2, "0")}`;
            const currentFragment = document.getElementById(fragmentId);

            if (!currentFragment || currentFragment.dataset.autoUpdate !== "true" || currentFragment.dataset.currentMonth === currentMonth) {
                return;
            }

            const url = new URL(calendarUrl, window.location.origin);
            const status = document.querySelector("select[name='status']")?.value;
            if (status) {
                url.searchParams.set("status", status);
            }

            const calendarResponse = await fetch(url, { cache: "no-store" });
            if (!calendarResponse.ok) {
                return;
            }

            const markup = await calendarResponse.text();
            const template = document.createElement("template");
            template.innerHTML = markup;
            const replacement = template.content.querySelector(`#${fragmentId}`);

            if (replacement?.dataset.currentMonth === currentMonth) {
                currentFragment.replaceWith(replacement);
            }
        } catch {
            // Keep the current calendar visible and retry on the next poll.
        } finally {
            requestInProgress = false;
        }
    }

    function scheduleNextCheck() {
        window.clearTimeout(pollTimer);
        pollTimer = window.setTimeout(async () => {
            await checkForMonthChange();
            scheduleNextCheck();
        }, pollIntervalMilliseconds);
    }

    document.addEventListener("visibilitychange", () => {
        if (document.visibilityState === "visible") {
            void checkForMonthChange();
        }
    });

    void checkForMonthChange();
    scheduleNextCheck();
})();

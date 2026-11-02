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

    function hasExplicitPlannedDate() {
        return Boolean(document.querySelector("input[name='plannedDate']")?.value);
    }

    function getSelectedStatus() {
        return document.querySelector("select[name='status']")?.value ?? "";
    }

    async function checkForMonthChange() {
        if (requestInProgress || document.visibilityState !== "visible") {
            return;
        }

        if (hasExplicitPlannedDate()) {
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

            if (hasExplicitPlannedDate()
                || !currentFragment
                || currentFragment.dataset.autoUpdate !== "true"
                || currentFragment.dataset.currentMonth === currentMonth) {
                return;
            }

            const requestedStatus = getSelectedStatus();
            const url = new URL(calendarUrl, window.location.origin);
            if (requestedStatus) {
                url.searchParams.set("status", requestedStatus);
            }

            const calendarResponse = await fetch(url, { cache: "no-store" });
            if (!calendarResponse.ok) {
                return;
            }

            const markup = await calendarResponse.text();
            const template = document.createElement("template");
            template.innerHTML = markup;
            const replacement = template.content.querySelector(`#${fragmentId}`);
            const latestFragment = document.getElementById(fragmentId);

            if (!hasExplicitPlannedDate()
                && latestFragment === currentFragment
                && latestFragment.dataset.autoUpdate === "true"
                && getSelectedStatus() === requestedStatus
                && replacement?.dataset.currentMonth === currentMonth) {
                latestFragment.replaceWith(replacement);
            }
        } catch {
            // Keep the current calendar visible and retry on the next poll.
        } finally {
            requestInProgress = false;
        }
    }

    function scheduleNextCheck() {
        window.clearTimeout(pollTimer);
        pollTimer = undefined;
        if (document.visibilityState !== "visible") {
            return;
        }

        pollTimer = window.setTimeout(async () => {
            await checkForMonthChange();
            scheduleNextCheck();
        }, pollIntervalMilliseconds);
    }

    document.addEventListener("visibilitychange", () => {
        if (document.visibilityState === "visible") {
            void checkForMonthChange();
            scheduleNextCheck();
        } else {
            window.clearTimeout(pollTimer);
            pollTimer = undefined;
        }
    });

    void checkForMonthChange();
    scheduleNextCheck();
})();

import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { test } from "node:test";
import vm from "node:vm";

const script = await readFile(new URL("../src/App/wwwroot/js/production-calendar.js", import.meta.url), "utf8");

function createHarness({ autoUpdate = true, currentMonth = "2031-06", visibilityState = "visible", fetch } = {}) {
    const calls = [];
    const timers = new Map();
    let nextTimerId = 1;
    let currentFragment = {
        dataset: {
            autoUpdate: String(autoUpdate),
            currentMonth,
            monthUrl: "/Production/CurrentMonth",
            calendarUrl: "/Production/Calendar"
        },
        replaceWith(replacement) {
            currentFragment = replacement;
        }
    };
    let visibilityHandler;
    const plannedDateInput = { value: "" };
    const statusInput = { value: "Planned" };
    const replacement = { dataset: { autoUpdate: "true", currentMonth: "2031-07" } };

    const document = {
        visibilityState,
        getElementById: () => currentFragment,
        querySelector(selector) {
            if (selector === "input[name='plannedDate']") return plannedDateInput;
            if (selector === "select[name='status']") return statusInput;
            return null;
        },
        createElement() {
            return { content: { querySelector: () => replacement }, set innerHTML(_markup) {} };
        },
        addEventListener(event, handler) {
            if (event === "visibilitychange") visibilityHandler = handler;
        }
    };
    const window = {
        location: { origin: "https://craftsman.example" },
        setTimeout(callback, delay) {
            const id = nextTimerId++;
            timers.set(id, { callback, delay });
            return id;
        },
        clearTimeout(id) {
            timers.delete(id);
        }
    };
    const fetchFunction = async (...args) => {
        calls.push(args);
        return fetch ? fetch(...args) : { ok: true, json: async () => ({ year: 2031, month: 6 }) };
    };

    vm.runInNewContext(script, { document, window, fetch: fetchFunction, URL });

    return {
        calls,
        timers,
        plannedDateInput,
        statusInput,
        currentFragment: () => currentFragment,
        setVisibility(state) {
            document.visibilityState = state;
            visibilityHandler();
        }
    };
}

const flushAsyncWork = () => new Promise(resolve => setImmediate(resolve));

test("does not poll when the page has an explicit date", () => {
    const harness = createHarness({ autoUpdate: false });

    assert.equal(harness.calls.length, 0);
    assert.equal(harness.timers.size, 0);
});

test("replaces the calendar after a month change and keeps the selected status", async () => {
    const harness = createHarness({
        fetch: async input => input.toString().includes("CurrentMonth")
            ? { ok: true, json: async () => ({ year: 2031, month: 7 }) }
            : { ok: true, text: async () => "<div id='production-calendar-fragment'></div>" }
    });

    await flushAsyncWork();

    assert.equal(harness.calls.length, 2);
    assert.equal(new URL(harness.calls[1][0]).searchParams.get("status"), "Planned");
    assert.equal(harness.currentFragment().dataset.currentMonth, "2031-07");
});

test("does not replace the calendar if a date is selected while checking the month", async () => {
    let resolveMonth;
    const monthResponse = new Promise(resolve => { resolveMonth = resolve; });
    const harness = createHarness({ fetch: () => monthResponse });

    harness.plannedDateInput.value = "2031-06-12";
    resolveMonth({ ok: true, json: async () => ({ year: 2031, month: 7 }) });
    await flushAsyncWork();

    assert.equal(harness.calls.length, 1);
    assert.equal(harness.currentFragment().dataset.currentMonth, "2031-06");
});

test("does not replace the calendar if a date is selected while loading the new fragment", async () => {
    let resolveCalendar;
    const calendarResponse = new Promise(resolve => { resolveCalendar = resolve; });
    const harness = createHarness({
        fetch: async input => input.toString().includes("CurrentMonth")
            ? { ok: true, json: async () => ({ year: 2031, month: 7 }) }
            : calendarResponse
    });
    const initialFragment = harness.currentFragment();

    await flushAsyncWork();
    harness.plannedDateInput.value = "2031-06-12";
    resolveCalendar({ ok: true, text: async () => "<div id='production-calendar-fragment'></div>" });
    await flushAsyncWork();

    assert.equal(harness.currentFragment(), initialFragment);
});

test("does not replace the calendar if the status filter changes while loading the fragment", async () => {
    let resolveCalendar;
    const calendarResponse = new Promise(resolve => { resolveCalendar = resolve; });
    const harness = createHarness({
        fetch: async input => input.toString().includes("CurrentMonth")
            ? { ok: true, json: async () => ({ year: 2031, month: 7 }) }
            : calendarResponse
    });
    const initialFragment = harness.currentFragment();

    await flushAsyncWork();
    harness.statusInput.value = "Completed";
    resolveCalendar({ ok: true, text: async () => "<div id='production-calendar-fragment'></div>" });
    await flushAsyncWork();

    assert.equal(harness.currentFragment(), initialFragment);
});

test("pauses timers while hidden and checks immediately when visible again", async () => {
    const harness = createHarness({ visibilityState: "hidden" });

    assert.equal(harness.calls.length, 0);
    assert.equal(harness.timers.size, 0);

    harness.setVisibility("visible");
    await flushAsyncWork();

    assert.equal(harness.calls.length, 1);
    assert.equal(harness.timers.size, 1);

    harness.setVisibility("hidden");
    assert.equal(harness.timers.size, 0);
});

test("keeps the existing calendar when the month request fails", async () => {
    const harness = createHarness({ fetch: async () => { throw new Error("network unavailable"); } });
    const initialFragment = harness.currentFragment();

    await flushAsyncWork();

    assert.equal(harness.currentFragment(), initialFragment);
    assert.equal(harness.timers.size, 1);
});

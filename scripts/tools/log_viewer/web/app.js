(function () {
  "use strict";

  const state = {
    offset: null,
    lines: [],
    carry: "",
    pollMs: 1200,
    initialTailBytes: 200000,
    maxLines: 12000,
    inFlight: false,
    paused: false,
    filePath: "",
  };

  const el = {
    status: document.getElementById("status"),
    filePath: document.getElementById("filePath"),
    stats: document.getElementById("stats"),
    searchInput: document.getElementById("searchInput"),
    caseSensitive: document.getElementById("caseSensitive"),
    autoScroll: document.getElementById("autoScroll"),
    clearFilter: document.getElementById("clearFilter"),
    pauseResume: document.getElementById("pauseResume"),
    refreshNow: document.getElementById("refreshNow"),
    logOutput: document.getElementById("logOutput"),
  };

  function setStatus(text) {
    el.status.textContent = text;
  }

  function appendChunk(content) {
    if (!content) {
      return;
    }
    const normalized = content.replace(/\r\n/g, "\n");
    const merged = state.carry + normalized;
    const parts = merged.split("\n");
    state.carry = parts.pop() || "";
    for (const line of parts) {
      state.lines.push(line);
    }
    if (state.lines.length > state.maxLines) {
      state.lines.splice(0, state.lines.length - state.maxLines);
    }
  }

  function getVisibleLines() {
    const query = el.searchInput.value;
    if (!query) {
      return state.lines;
    }
    const caseSensitive = el.caseSensitive.checked;
    const needle = caseSensitive ? query : query.toLowerCase();
    return state.lines.filter((line) => {
      const hay = caseSensitive ? line : line.toLowerCase();
      return hay.includes(needle);
    });
  }

  function render() {
    const visibleLines = getVisibleLines();
    el.logOutput.textContent = visibleLines.join("\n");
    const offsetText = state.offset === null ? "?" : String(state.offset);
    el.stats.textContent =
      "showing " +
      visibleLines.length +
      " of " +
      state.lines.length +
      " lines, offset " +
      offsetText;

    if (el.autoScroll.checked) {
      el.logOutput.scrollTop = el.logOutput.scrollHeight;
    }
  }

  async function fetchConfig() {
    const response = await fetch("/api/config", { cache: "no-store" });
    if (!response.ok) {
      throw new Error("Failed to load config: HTTP " + response.status);
    }
    const data = await response.json();
    state.pollMs = data.poll_ms || state.pollMs;
    state.initialTailBytes = data.initial_tail_bytes || state.initialTailBytes;
    state.maxLines = data.max_lines || state.maxLines;
    state.filePath = data.log_path || "";
    el.filePath.textContent = state.filePath || "-";
  }

  async function pollLog(force) {
    if (state.inFlight) {
      return;
    }
    if (state.paused && !force) {
      return;
    }

    state.inFlight = true;
    try {
      const params = new URLSearchParams();
      if (state.offset === null) {
        params.set("tail", String(state.initialTailBytes));
      } else {
        params.set("offset", String(state.offset));
      }

      const response = await fetch("/api/log?" + params.toString(), { cache: "no-store" });
      if (!response.ok) {
        throw new Error("Failed polling log: HTTP " + response.status);
      }

      const data = await response.json();
      if (!data.exists) {
        setStatus("Log file not found: " + (data.path || state.filePath));
        return;
      }

      if (data.reset) {
        state.lines = [];
        state.carry = "";
      }

      state.offset = data.offset;
      appendChunk(data.content || "");
      render();

      if (state.paused) {
        setStatus("Paused");
      } else if (data.truncated) {
        setStatus("Connected (catching up...)");
      } else {
        setStatus("Connected");
      }
    } catch (error) {
      setStatus("Error: " + String(error));
    } finally {
      state.inFlight = false;
    }
  }

  function startPolling() {
    setInterval(() => {
      void pollLog(false);
    }, state.pollMs);
  }

  function bindEvents() {
    el.searchInput.addEventListener("input", () => render());
    el.caseSensitive.addEventListener("change", () => render());
    el.clearFilter.addEventListener("click", () => {
      el.searchInput.value = "";
      render();
      el.searchInput.focus();
    });
    el.refreshNow.addEventListener("click", () => {
      void pollLog(true);
    });
    el.pauseResume.addEventListener("click", () => {
      state.paused = !state.paused;
      el.pauseResume.textContent = state.paused ? "Resume" : "Pause";
      setStatus(state.paused ? "Paused" : "Connected");
      if (!state.paused) {
        void pollLog(true);
      }
    });
    document.addEventListener("keydown", (event) => {
      if (event.ctrlKey && event.key.toLowerCase() === "f") {
        event.preventDefault();
        el.searchInput.focus();
        el.searchInput.select();
      }
    });
  }

  async function init() {
    bindEvents();
    await fetchConfig();
    await pollLog(true);
    startPolling();
  }

  void init();
})();

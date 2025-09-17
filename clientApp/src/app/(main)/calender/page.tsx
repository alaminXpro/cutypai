"use client";

import React, { useEffect, useMemo, useRef, useState } from "react";

// ---------- Types ----------
type DayKey = string; // YYYY-MM-DD
type HistoryMap = Record<DayKey, number>; // 0 = inactive, 1..4 intensity
type NotesMap = Record<DayKey, string>;

// ---------- Helpers ----------
const toDateKey = (d: Date) => d.toISOString().slice(0, 10);
const pad = (n: number) => String(n).padStart(2, "0");

const DAYS = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
const MONTHS = [
  "Jan",
  "Feb",
  "Mar",
  "Apr",
  "May",
  "Jun",
  "Jul",
  "Aug",
  "Sep",
  "Oct",
  "Nov",
  "Dec",
];

const STORAGE_KEY = "activity_calendar_history_v1";
const NOTES_KEY = "activity_calendar_notes_v1";
const MAX_LEVEL = 4; // 0 (inactive) .. 4 (very active)

// Color for levels (0..MAX_LEVEL)
const levelColor = (level: number) => {
  if (level <= 0) return "#ebedf0"; // light gray
  const shades = ["#9be9a8", "#40c463", "#30a14e", "#216e39"]; // 1..4
  return shades[Math.min(Math.max(level - 1, 0), shades.length - 1)];
};

// ---------- Core logic helpers ----------
function getStartOfYear(year: number) {
  return new Date(year, 0, 1);
}
function getEndOfYear(year: number) {
  return new Date(year, 11, 31);
}

function iterateDays(start: Date, end: Date) {
  const arr: Date[] = [];
  let cur = new Date(start);
  while (cur <= end) {
    arr.push(new Date(cur));
    cur.setDate(cur.getDate() + 1);
  }
  return arr;
}

// Build weeks for a year as a 2D array [weekIndex][weekdayIndex]
function buildYearWeeks(year: number) {
  const start = new Date(year, 0, 1);
  const end = new Date(year, 11, 31);
  // Find the Sunday on or before Jan 1
  const firstSunday = new Date(start);
  firstSunday.setDate(start.getDate() - start.getDay());
  const days = iterateDays(firstSunday, end);
  const weeks: (Date | null)[][] = [];
  for (let i = 0; i < days.length; i += 7) {
    const week: (Date | null)[] = [];
    for (let j = 0; j < 7; j++) {
      const idx = i + j;
      week.push(days[idx] ?? null);
    }
    weeks.push(week);
  }
  return weeks;
}

// ---------- Rating algorithm ----------
function computeRating(history: HistoryMap, year: number) {
  const start = getStartOfYear(year);
  const end = getEndOfYear(year);
  const days = iterateDays(start, end);
  if (days.length === 0) return 0;

  let sum = 0;
  let activeDays = 0;
  let recentSum = 0;
  const today = new Date();
  const recentStart = new Date(today);
  recentStart.setDate(today.getDate() - 29);

  for (const d of days) {
    const key = toDateKey(d);
    const lvl = history[key] ?? 0;
    sum += lvl;
    if (lvl > 0) activeDays++;
    if (d >= recentStart && d <= today) recentSum += lvl;
  }

  const avgIntensity = sum / days.length / MAX_LEVEL; // 0..1
  const consistency = activeDays / days.length; // 0..1
  const recentAvg = recentSum / 30 / MAX_LEVEL; // 0..1

  const score =
    0.5 * avgIntensity + // long-term overall activity
    0.35 * consistency + // consistency
    0.15 * recentAvg; // recency

  return Math.round(Math.min(1, score) * 100);
}

// ---------- Components ----------
function IconSpark() {
  return (
    <span className="inline-flex items-center justify-center h-10 w-10 rounded-full bg-gradient-to-br from-indigo-500 to-pink-500 shadow-md mr-3">
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" aria-hidden>
        <path
          d="M12 2v2m0 16v2m8-10h2M2 12H0m17.657-7.657l1.414 1.414M4.929 19.071l-1.414 1.414M19.071 19.071l-1.414-1.414M4.929 4.929L3.515 3.515"
          stroke="#fff"
          strokeWidth="1.5"
          strokeLinecap="round"
        />
      </svg>
    </span>
  );
}

export default function ActivityCalendarPage() {
  const now = new Date();
  const [year, setYear] = useState<number>(now.getFullYear());
  const [selectedDate, setSelectedDate] = useState<Date | null>(now);
  const [history, setHistory] = useState<HistoryMap>({});
  const [notes, setNotes] = useState<NotesMap>({});
  const [showHeatmapMonths, setShowHeatmapMonths] = useState(true);
  const [showNoteIconsOnHeatmap, setShowNoteIconsOnHeatmap] = useState(true); // new toggle

  // Note modal state
  const [noteModalOpen, setNoteModalOpen] = useState(false);
  const [noteModalDate, setNoteModalDate] = useState<DayKey | null>(null);
  const [noteModalText, setNoteModalText] = useState("");
  const modalTextareaRef = useRef<HTMLTextAreaElement | null>(null);

  // load from localStorage
  useEffect(() => {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) setHistory(JSON.parse(raw));
    } catch (e) {
      // ignore
    }
    try {
      const rawNotes = localStorage.getItem(NOTES_KEY);
      if (rawNotes) setNotes(JSON.parse(rawNotes));
    } catch (e) {
      // ignore
    }
  }, []);
  useEffect(() => {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(history));
    } catch (e) {}
  }, [history]);
  useEffect(() => {
    try {
      localStorage.setItem(NOTES_KEY, JSON.stringify(notes));
    } catch (e) {}
  }, [notes]);

  const weeks = useMemo(() => buildYearWeeks(year), [year]);

  const setDayLevel = (date: Date, level: number) => {
    const key = toDateKey(date);
    setHistory((prev) => ({ ...prev, [key]: level }));
  };

  const cycleDay = (date: Date) => {
    const key = toDateKey(date);
    const cur = history[key] ?? 0;
    const next = (cur + 1) % (MAX_LEVEL + 1);
    setHistory((prev) => ({ ...prev, [key]: next }));
  };

  const clearDay = (date: Date) => {
    const key = toDateKey(date);
    setHistory((prev) => {
      const copy = { ...prev };
      delete copy[key];
      return copy;
    });
  };

  // note handlers
  const openNoteModal = (dateKey: DayKey) => {
    setNoteModalDate(dateKey);
    setNoteModalText(notes[dateKey] ?? "");
    setNoteModalOpen(true);
    setTimeout(() => {
      modalTextareaRef.current?.focus();
    }, 0);
  };

  const closeNoteModal = () => {
    setNoteModalOpen(false);
    setNoteModalDate(null);
    setNoteModalText("");
  };

  const saveNote = () => {
    if (!noteModalDate) return;
    const text = noteModalText;
    setNotes((prev) => {
      const copy = { ...prev };
      if (text.length === 0) {
        delete copy[noteModalDate];
      } else {
        copy[noteModalDate] = text;
      }
      return copy;
    });
    closeNoteModal();
  };

  const deleteNote = (dateKey: DayKey) => {
    setNotes((prev) => {
      const copy = { ...prev };
      delete copy[dateKey];
      return copy;
    });
  };

  // summary metrics
  const metrics = useMemo(() => {
    const start = getStartOfYear(year);
    const end = getEndOfYear(year);
    const days = iterateDays(start, end);

    let yearSum = 0;
    const monthlySum = Array(12).fill(0);
    let longestStreak = 0;
    let curStreak = 0;

    for (const d of days) {
      const key = toDateKey(d);
      const lvl = history[key] ?? 0;
      yearSum += lvl;
      monthlySum[d.getMonth()] += lvl > 0 ? 1 : 0; // count active days in month

      if (lvl > 0) {
        curStreak += 1;
        longestStreak = Math.max(longestStreak, curStreak);
      } else {
        curStreak = 0;
      }
    }

    // compute current ongoing streak up to today
    let ongoing = 0;
    const today = new Date();
    for (let d = new Date(today); ; ) {
      const key = toDateKey(d);
      if ((history[key] ?? 0) > 0) {
        ongoing++;
        d.setDate(d.getDate() - 1);
        if (d < start) break;
      } else break;
    }

    const lastMonth = new Date().getMonth() === 0 ? 11 : new Date().getMonth() - 1;

    return {
      yearSum,
      monthlyActiveDays: monthlySum,
      longestStreak,
      ongoingStreak: ongoing,
      lastMonthActiveDays: monthlySum[lastMonth] ?? 0,
      totalActiveDaysInYear: Object.values(history).filter((v) => v > 0).length,
    };
  }, [history, year]);

  const rating = useMemo(() => computeRating(history, year), [history, year]);

  // export/import
  const exportJSON = () => {
    const data = JSON.stringify(history, null, 2);
    const blob = new Blob([data], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `activity_history_${year}.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const importJSON = (file: File | null) => {
    if (!file) return;
    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const parsed = JSON.parse(String(e.target?.result ?? "{}"));
        setHistory((prev) => ({ ...prev, ...parsed }));
      } catch (err) {
        alert("Invalid file");
      }
    };
    reader.readAsText(file);
  };

  // export/import notes
  const exportNotes = () => {
    const data = JSON.stringify(notes, null, 2);
    const blob = new Blob([data], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `activity_notes_${year}.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const importNotes = (file: File | null) => {
    if (!file) return;
    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const parsed = JSON.parse(String(e.target?.result ?? "{}"));
        setNotes((prev) => ({ ...prev, ...parsed }));
      } catch (err) {
        alert("Invalid file");
      }
    };
    reader.readAsText(file);
  };

  // combined export (history + notes)
  const exportAll = () => {
    const combined = { history, notes };
    const data = JSON.stringify(combined, null, 2);
    const blob = new Blob([data], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `activity_all_${year}.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  // small UI components

  // Activity History item with inline expand & quick actions (new UI)
  function NoteListItem({ date, lvl, note }: { date: string; lvl: number; note?: string }) {
    const [expanded, setExpanded] = useState(false);

    return (
      <li
        key={date}
        className="flex items-start justify-between p-2 rounded-lg hover:bg-slate-50 transition-shadow duration-150"
        role="listitem"
      >
        <div className="flex items-start gap-3 min-w-0">
          <div
            style={{ background: levelColor(lvl) }}
            className="h-6 w-6 rounded-md border border-slate-200 shadow-sm flex-shrink-0"
            aria-hidden
          />
          <div className="flex-1 min-w-0">
            <div className="flex items-center justify-between gap-3">
              <div className="font-medium text-slate-700 tabular-nums">{date}</div>
              <div className="text-xs text-slate-500">level {lvl}</div>
            </div>

            {note ? (
              <>
                <div
                  className={`mt-1 text-xs text-slate-600 transition-all ${expanded ? "max-h-[400px]" : "max-h-[40px] overflow-hidden"}`}
                >
                  {note}
                </div>

                <div className="mt-2 flex gap-2">
                  <button
                    onClick={() => setExpanded((s) => !s)}
                    className="text-xs px-2 py-1 rounded border bg-white"
                    aria-expanded={expanded}
                  >
                    {expanded ? "Collapse" : "Preview"}
                  </button>
                  <button
                    onClick={() => openNoteModal(date)}
                    className="text-xs px-2 py-1 rounded border bg-white"
                    aria-label={`Edit note for ${date}`}
                  >
                    Edit
                  </button>
                  <button
                    onClick={() => {
                      if (!confirm("Delete note?")) return;
                      deleteNote(date);
                    }}
                    className="text-xs px-2 py-1 rounded border bg-red-50 text-red-700"
                    aria-label={`Delete note for ${date}`}
                  >
                    Delete
                  </button>
                </div>
              </>
            ) : (
              <div className="mt-1 text-xs text-slate-400">No note</div>
            )}
          </div>
        </div>
      </li>
    );
  }

  function HeatmapYear() {
    return (
      <div>
        <div className="flex items-center justify-between mb-3">
          <div className="flex items-center">
            <IconSpark />
            <div>
              <div className="text-lg font-bold text-slate-700">Yearly Activity</div>
              <div className="text-sm text-slate-500">Heatmap overview of your activity</div>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <select
              value={year}
              onChange={(e) => setYear(Number(e.target.value))}
              className="border rounded px-2 py-1 text-sm bg-white"
              aria-label="Select year"
            >
              {Array.from({ length: 6 }).map((_, i) => {
                const y = now.getFullYear() - (2 - i);
                return (
                  <option key={y} value={y}>
                    {y}
                  </option>
                );
              })}
            </select>

            <button
              onClick={() => setShowHeatmapMonths((s) => !s)}
              className="text-sm px-3 py-1 border rounded bg-white hover:shadow-sm transition"
              aria-pressed={!showHeatmapMonths}
            >
              Toggle month labels
            </button>

            <button
              onClick={() => setShowNoteIconsOnHeatmap((s) => !s)}
              className="text-sm px-3 py-1 border rounded bg-white hover:shadow-sm transition"
              aria-pressed={showNoteIconsOnHeatmap}
              title="Toggle note icons on heatmap"
            >
              {showNoteIconsOnHeatmap ? "Hide notes" : "Show notes"}
            </button>

            <button onClick={exportJSON} className="text-sm px-3 py-1 border rounded bg-white hover:shadow-sm transition" title="Export history">
              Export
            </button>

            <label className="text-sm px-3 py-1 border rounded cursor-pointer bg-white hover:shadow-sm transition" title="Import history">
              Import
              <input
                type="file"
                accept="application/json"
                onChange={(e) => importJSON(e.target.files?.[0] ?? null)}
                className="hidden"
                aria-hidden
              />
            </label>

            {/* notes export/import */}
            <button onClick={exportNotes} className="text-sm px-3 py-1 border rounded bg-white hover:shadow-sm transition" title="Export notes">
              Export Notes
            </button>
            <label className="text-sm px-3 py-1 border rounded cursor-pointer bg-white hover:shadow-sm transition" title="Import notes">
              Import Notes
              <input type="file" accept="application/json" onChange={(e) => importNotes(e.target.files?.[0] ?? null)} className="hidden" />
            </label>

            {/* combined export */}
            <button onClick={exportAll} className="text-sm px-3 py-1 border rounded bg-emerald-50 text-emerald-700 hover:shadow-sm transition" title="Export history + notes">
              Export All
            </button>
          </div>
        </div>

        <div className="flex gap-4 items-start overflow-x-auto pb-4">
          {/* weekday labels vertical */}
          <div className="flex flex-col gap-1 mr-2 text-xs text-slate-500 shrink-0">
            {DAYS.map((d) => (
              <div key={d} className="h-3 w-6 text-center select-none">
                {d[0]}
              </div>
            ))}
          </div>

          <div className="flex gap-1">
            {weeks.map((week, wi) => (
              <div key={wi} className="grid grid-rows-7 gap-1">
                {week.map((d, di) => {
                  if (!d || d.getFullYear() !== year) {
                    return <div key={di} className="h-3 w-3 rounded" />;
                  }
                  const key = toDateKey(d);
                  const lvl = history[key] ?? 0;
                  const hasNote = Boolean(notes[key]);
                  return (
                    <div key={di} className="relative">
                      <button
                        title={`${key} — level ${lvl}${hasNote ? " — has note" : ""}`}
                        onClick={() => cycleDay(d)}
                        onDoubleClick={() => clearDay(d)}
                        style={{ background: levelColor(lvl) }}
                        className="h-3 w-3 rounded cursor-pointer border border-slate-200 focus:outline-none focus:ring-2 focus:ring-offset-1 focus:ring-sky-300 transition"
                        aria-label={`${key}, level ${lvl}${hasNote ? ", has note" : ""}`}
                      />
                      {/* simple dot to indicate note (toggleable) */}
                      {showNoteIconsOnHeatmap && hasNote && (
                        <div
                          className="absolute -top-1 -right-1 h-2 w-2 rounded-full border border-white shadow"
                          style={{ background: "#f59e0b" }}
                          title={notes[key]?.slice(0, 80) ?? "Note"}
                        />
                      )}
                    </div>
                  );
                })}
              </div>
            ))}
          </div>

          {/* legend */}
          <div className="ml-4 flex items-center gap-2 shrink-0">
            <div className="text-xs text-slate-500 mr-2">Less</div>
            {Array.from({ length: MAX_LEVEL + 1 }).map((_, i) => (
              <div
                key={i}
                className="h-4 w-4 rounded border border-slate-200"
                style={{ background: levelColor(i) }}
                title={`level ${i}`}
                aria-hidden
              />
            ))}
            <div className="text-xs text-slate-500 ml-2">More</div>
          </div>
        </div>
      </div>
    );
  }

  function MonthCalendar({ monthIndex }: { monthIndex: number }) {
    const monthStart = new Date(year, monthIndex, 1);
    const daysInMonth = new Date(year, monthIndex + 1, 0).getDate();
    const firstDay = monthStart.getDay();

    const cells: (Date | null)[] = [];
    for (let i = 0; i < firstDay; i++) cells.push(null);
    for (let d = 1; d <= daysInMonth; d++) cells.push(new Date(year, monthIndex, d));

    return (
      <div className="border rounded-xl p-3 bg-white hover:shadow-md transition transform hover:-translate-y-0.5">
        <div className="flex items-center justify-between mb-2">
          <div>
            <div className="font-semibold text-slate-700">
              {MONTHS[monthIndex]} {year}
            </div>
            <div className="text-xs text-slate-500">Active: {metrics.monthlyActiveDays[monthIndex] ?? 0} days</div>
          </div>
          <div className="text-sm text-slate-600">{metrics.yearSum} pts</div>
        </div>

        <div className="grid grid-cols-7 gap-1 text-xs select-none mb-2">
          {DAYS.map((d) => (
            <div key={d} className="text-center text-[10px] text-slate-400">
              {d}
            </div>
          ))}
        </div>

        <div className="grid grid-cols-7 gap-1">
          {cells.map((dt, idx) => {
            if (!dt) return <div key={idx} className="h-10 w-10" />;
            const key = toDateKey(dt);
            const lvl = history[key] ?? 0;
            const isToday = key === toDateKey(new Date());
            const isSelected = selectedDate ? toDateKey(selectedDate) === key : false;
            const hasNote = Boolean(notes[key]);
            const notePreview = notes[key]?.slice(0, 120) ?? "";

            return (
              <div key={key} className="relative group">
                <button
                  onClick={() => {
                    cycleDay(dt);
                    setSelectedDate(dt);
                  }}
                  onDoubleClick={() => clearDay(dt)}
                  className={`h-10 w-10 flex items-center justify-center rounded-md cursor-pointer border
                    focus:outline-none focus:ring-2 focus:ring-offset-1 transition
                  `}
                  style={{
                    background: levelColor(lvl),
                    boxShadow: isSelected ? "inset 0 0 0 2px rgba(59,130,246,0.12)" : undefined,
                  }}
                  title={`${key} — level ${lvl}${hasNote ? " — has note" : ""}`}
                  aria-pressed={lvl > 0}
                  aria-label={`${MONTHS[monthIndex]} ${dt.getDate()}, ${year}. level ${lvl}${hasNote ? ", has note" : ""}`}
                >
                  <div className={`text-sm font-medium ${lvl === 0 ? "text-slate-700" : "text-slate-900"}`}>
                    <span className={isToday ? "underline decoration-sky-300" : ""}>{dt.getDate()}</span>
                  </div>
                </button>

                {/* improved note button: visible on hover (group-hover) or always if hasNote */}
                <button
                  onClick={exportNotes => {
                    exportNotes.stopPropagation();
                    openNoteModal(key);
                  }}
                  title={hasNote ? "Edit note" : "Add note"}
                  className={`absolute -top-1 -right-1 h-5 w-5 rounded-full flex items-center justify-center text-xs border bg-white shadow-sm transition
                    ${hasNote ? "ring-2 ring-amber-200" : "opacity-0 group-hover:opacity-100"}
                  `}
                  aria-label={hasNote ? `Edit note for ${key}` : `Add note for ${key}`}
                >
                  {hasNote ? (
                    // pencil icon (existing note)
                    <svg className="w-3 h-3 text-amber-700" viewBox="0 0 24 24" fill="none" aria-hidden>
                      <path d="M3 21v-3.75L14.81 5.44a2 2 0 0 1 2.83 0l.92.92a2 2 0 0 1 0 2.83L6.75 21H3z" stroke="currentColor" strokeWidth="1" strokeLinecap="round" strokeLinejoin="round"/>
                    </svg>
                  ) : (
                    // small dot when hover (add)
                    <svg className="w-3 h-3 text-slate-400" viewBox="0 0 24 24" fill="none" aria-hidden>
                      <circle cx="12" cy="12" r="3" fill="currentColor" />
                    </svg>
                  )}
                </button>

                {/* tooltip-like preview on hover (pure CSS, appears above cell) */}
                {hasNote && (
                  <div
                    className="pointer-events-none absolute -top-14 left-1/2 -translate-x-1/2 w-44 bg-white border rounded shadow-lg p-2 text-xs text-slate-700 opacity-0 group-hover:opacity-100 transition-opacity"
                    role="note"
                    aria-hidden={!hasNote}
                    title={notePreview}
                  >
                    <div className="font-medium text-[12px] mb-1">Note</div>
                    <div className="text-[12px] leading-snug">{notePreview}{notes[key]!.length > 120 ? "…" : ""}</div>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>
    );
  }

  // Note modal (improved UI)
  function NoteModal() {
    if (!noteModalOpen || !noteModalDate) return null;
    const previewDate = noteModalDate;
    const existing = notes[previewDate] ?? "";
    return (
      <div
        className="fixed inset-0 z-50 flex items-center justify-center p-4"
        role="dialog"
        aria-modal="true"
        aria-labelledby="note-modal-title"
      >
        <div className="absolute inset-0 bg-black/40" onClick={closeNoteModal} />
        <div className="relative bg-white rounded-xl shadow-xl max-w-lg w-full p-4">
          <div className="flex items-start justify-between gap-4">
            <div>
              <h3 id="note-modal-title" className="text-lg font-semibold text-slate-800">
                Note — {previewDate}
              </h3>
              <div className="text-xs text-slate-500">Attach a reminder or important info to this date</div>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={() => {
                  if (!noteModalDate) return;
                  if (!confirm("Delete this note?")) return;
                  deleteNote(noteModalDate);
                  closeNoteModal();
                }}
                className="text-sm px-3 py-1 rounded bg-red-50 text-red-700 border border-red-100"
                aria-label="Delete note"
              >
                Delete
              </button>
              <button
                onClick={closeNoteModal}
                className="text-sm px-3 py-1 rounded border bg-white"
                aria-label="Close note dialog"
              >
                Close
              </button>
            </div>
          </div>

          <div className="mt-3">
            <textarea
              ref={modalTextareaRef}
              value={noteModalText}
              onChange={(e) => setNoteModalText(e.target.value)}
              placeholder="Write your note here... (eg. Meeting at 3pm, groceries, ideas, links, etc.)"
              className="w-full min-h-[160px] p-3 border rounded focus:outline-none focus:ring-2 focus:ring-amber-200 resize-y text-sm"
              aria-label={`Note for ${previewDate}`}
            />
            <div className="mt-2 text-xs text-slate-400 flex justify-between items-center">
              <div>{noteModalText.length} characters</div>
              <div className="text-xs text-slate-500">{existing ? "Editing existing note" : "New note"}</div>
            </div>
          </div>

          <div className="mt-3 flex justify-end gap-2">
            <button
              onClick={saveNote}
              className="px-4 py-2 rounded bg-amber-600 text-white text-sm shadow hover:brightness-105"
            >
              Save
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="p-6 max-w-7xl mx-auto">
      <div className="flex flex-col md:flex-row items-start md:items-center justify-between mb-6 gap-4">
        <div className="flex items-center mb-1 md:mb-0">
          <IconSpark />
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900">Activity Calendar</h1>
            <p className="text-sm text-slate-500">Track daily activity, view heatmap, and pin notes to dates</p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <div className="text-sm text-slate-600 hidden sm:block">Year</div>
          <select
            value={year}
            onChange={(e) => setYear(Number(e.target.value))}
            className="border px-2 py-1 rounded bg-white"
            aria-label="Select year"
          >
            {Array.from({ length: 6 }).map((_, i) => {
              const y = now.getFullYear() - (2 - i);
              return <option key={y} value={y}>{y}</option>;
            })}
          </select>

          <div className="inline-flex gap-2">
            <button
              onClick={() => {
                setHistory({});
                localStorage.removeItem(STORAGE_KEY);
              }}
              className="px-3 py-1 border rounded text-sm bg-white hover:shadow-sm transition"
              aria-label="Reset history"
            >
              Reset
            </button>
            <button
              onClick={() => {
                const key = toDateKey(new Date());
                setHistory((h) => ({ ...h, [key]: MAX_LEVEL }));
              }}
              className="px-3 py-1 border rounded text-sm bg-white hover:shadow-sm transition"
              aria-label="Quick mark today"
            >
              Quick Mark Today
            </button>

            {/* Quick add note for currently selected date */}
            <button
              onClick={() => {
                const key = selectedDate ? toDateKey(selectedDate) : toDateKey(new Date());
                openNoteModal(key);
              }}
              className="px-3 py-1 border rounded text-sm bg-amber-50 text-amber-700 hover:shadow-sm transition"
              aria-label="Quick add note for selected date"
              title="Add a note to the selected date"
            >
              Quick Note
            </button>
          </div>
        </div>
      </div>

      {/* top heatmap + stats */}
      <div className="grid md:grid-cols-3 gap-4 mb-6">
        <div className="md:col-span-2 bg-white p-4 rounded-2xl shadow">
          <HeatmapYear />
        </div>

        <div className="bg-white p-4 rounded-2xl shadow flex flex-col justify-between">
          <div>
            <div className="flex items-center justify-between">
              <div>
                <div className="text-sm text-slate-500">Totals</div>
                <div className="text-2xl font-bold text-slate-800 mt-2">{metrics.yearSum}</div>
                <div className="text-xs text-slate-400">intensity points this year</div>
              </div>

              {/* notes count badge */}
              <div className="inline-flex items-center gap-2">
                <div className="text-xs text-slate-500">Notes</div>
                <div className="inline-flex items-center px-2 py-1 bg-amber-50 text-amber-700 rounded-full text-sm font-medium border border-amber-100">
                  {Object.keys(notes).length}
                </div>
              </div>
            </div>

            <div className="mt-4 grid grid-cols-2 gap-2">
              <div>
                <div className="text-sm font-medium">Active days</div>
                <div className="text-lg font-semibold">{metrics.totalActiveDaysInYear}</div>
              </div>
              <div>
                <div className="text-sm font-medium">Longest streak</div>
                <div className="text-lg font-semibold">{metrics.longestStreak} days</div>
              </div>
            </div>
          </div>

          <div className="mt-4">
            <div className="text-sm text-slate-500">Credit Score</div>
            <div className="w-full bg-gray-100 h-4 rounded overflow-hidden mt-2">
              <div
                className="h-4 rounded"
                style={{ width: `${rating}%`, background: `linear-gradient(90deg,#34d399,#15803d)` }}
              />
            </div>
            <div className="mt-2 text-sm font-semibold">{rating} / 100</div>
            <div className="text-xs text-slate-400 mt-1">Based on intensity, consistency and recent activity</div>
          </div>
        </div>
      </div>

      {/* month view + history */}
      <div className="grid md:grid-cols-3 gap-6">
        <div className="md:col-span-2 grid grid-cols-3 gap-4">
          {Array.from({ length: 12 }).map((_, mi) => (
            <MonthCalendar key={mi} monthIndex={mi} />
          ))}
        </div>

        <aside
          className="bg-gradient-to-br from-white to-slate-50 p-5 rounded-3xl shadow-xl border border-slate-100"
          aria-labelledby="activity-history-heading"
        >
          <div className="flex items-start justify-between gap-3 mb-4">
            <div>
              <div id="activity-history-heading" className="text-sm text-slate-500">Activity History</div>
              <div className="text-xl font-bold tracking-tight text-slate-800">Recent active days</div>
            </div>

            <div className="flex flex-col items-end text-right">
              <div className="text-xs text-slate-400">Tip</div>
              <div
                className="mt-1 text-xs inline-flex items-center gap-2 px-2 py-1 rounded-full bg-slate-100 text-slate-600"
                title="Click a square to cycle level; double-click to clear; click the small icon to edit/add a note"
              >
                <svg className="w-3 h-3" viewBox="0 0 24 24" fill="none" aria-hidden>
                  <path d="M12 5v7l4 2" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
                </svg>
                click a square to cycle level
              </div>
            </div>
          </div>

          <div className="mt-2">
            <ul className="max-h-96 overflow-auto text-sm space-y-2 pr-2" aria-live="polite" role="list">
              {Object.entries(history)
                .filter(([_, v]) => v > 0)
                .sort((a, b) => (a[0] < b[0] ? 1 : -1))
                .slice(0, 200)
                .map(([date, lvl]) => (
                  <NoteListItem key={date} date={date} lvl={lvl} note={notes[date]} />
                ))}

              {Object.values(history).every((v) => v === 0) && (
                <li className="text-slate-400 py-2">No activity recorded yet</li>
              )}
            </ul>
          </div>

          <div className="mt-5 flex flex-wrap gap-3">
            <button
              onClick={() => {
                navigator.clipboard?.writeText(JSON.stringify(history, null, 2));
                alert("Copied JSON to clipboard");
              }}
              className="inline-flex items-center gap-2 px-3 py-2 rounded-lg border border-slate-200 bg-white text-sm shadow-sm hover:shadow-md transition transform hover:-translate-y-0.5 focus:outline-none focus:ring-2 focus:ring-slate-200"
              aria-label="Copy history JSON"
            >
              <svg className="w-4 h-4" viewBox="0 0 24 24" fill="none" aria-hidden>
                <path d="M9 12H3v8a1 1 0 0 0 1 1h8v-6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
                <rect x="7" y="3" width="14" height="14" rx="2" stroke="currentColor" strokeWidth="1.5" />
              </svg>
              Copy JSON
            </button>

            <button
              onClick={() => {
                const key = toDateKey(new Date());
                setHistory((h) => ({ ...h, [key]: MAX_LEVEL }));
              }}
              className="inline-flex items-center gap-2 px-3 py-2 rounded-lg border border-slate-200 bg-gradient-to-b from-sky-50 to-white text-sm shadow-sm hover:shadow-md transition transform hover:-translate-y-0.5 focus:outline-none focus:ring-2 focus:ring-sky-200"
              aria-label="Quick mark today"
            >
              <svg className="w-4 h-4" viewBox="0 0 24 24" fill="none" aria-hidden>
                <path d="M12 2v6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
                <path d="M6 10h12" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
                <rect x="3" y="6" width="18" height="14" rx="2" stroke="currentColor" strokeWidth="1.5" />
              </svg>
              Quick Mark Today
            </button>

            <button
              onClick={() => {
                if (!confirm("Reset all data?")) return;
                setHistory({});
                setNotes({});
                localStorage.removeItem(STORAGE_KEY);
                localStorage.removeItem(NOTES_KEY);
              }}
              className="inline-flex items-center gap-2 px-3 py-2 rounded-lg border border-red-100 bg-red-50 text-sm text-red-700 shadow-sm hover:shadow-md transition transform hover:-translate-y-0.5 focus:outline-none focus:ring-2 focus:ring-red-200"
              aria-label="Reset all history"
            >
              <svg className="w-4 h-4" viewBox="0 0 24 24" fill="none" aria-hidden>
                <path d="M3 6h18" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
                <path d="M8 6v12a2 2 0 0 0 2 2h4a2 2 0 0 0 2-2V6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
                <path d="M10 11v6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
                <path d="M14 11v6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
              </svg>
              Reset All
            </button>
          </div>
        </aside>
      </div>

      <div className="mt-8 text-sm text-slate-500">
        <h3 className="font-semibold mb-2 text-slate-800">How the rating works</h3>
        <ol className="list-decimal ml-5 space-y-1">
          <li>
            Each day stores a level (0 inactive — 4 very active). The calendar sums intensity across the year.
          </li>
          <li>
            The rating (0-100) combines average intensity, consistency (fraction of days active), and recent 30-day activity.
          </li>
          <li>
            You can click any small square in the heatmap to cycle through levels. Double-click clears the day. Click the small icon on a day to add or edit a note.
          </li>
        </ol>
      </div>

      {/* note modal rendered at root level to overlay everything */}
      <NoteModal />
    </div>
  );
}

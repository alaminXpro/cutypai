"use client";

import React, { useState, ChangeEvent } from "react";
import { useSearchParams } from "next/navigation";

// ---------- Types ----------
type BillingInfo = {
  name: string;
  email: string;
  address: string;
  city: string;
  zip: string;
  country: string;
};

type CardInfo = {
  number: string;
  name: string;
  expiry: string;
  cvc: string;
};

type PlanData = {
  title: string;
  subtitle: string;
  description: string;
  features: string[];
};

// ---------- Main Billing Page ----------
export default function BillingPage() {
  const searchParams = useSearchParams();
  let plan: PlanData | null = null;

  try {
    const planParam = searchParams.get("plan");
    if (planParam) plan = JSON.parse(decodeURIComponent(planParam));
  } catch {
    plan = null;
  }

  const selectedPlan = plan ? plan.title : "No plan selected";
  const price = plan?.subtitle ? plan.subtitle.replace(/[^0-9.]/g, "") : "0";

  const [billing, setBilling] = useState<BillingInfo>({
    name: "Md. Al Amin",
    email: "alamin.cse.20220104154@aust.edu",
    address: "Ahsanullah University of Science and Technology",
    city: "Dhaka",
    zip: "1207",
    country: "Bangladesh",
  });

  const [card, setCard] = useState<CardInfo>({
    number: "",
    name: "",
    expiry: "",
    cvc: "",
  });

  const handleBillingChange =
    (k: keyof BillingInfo) =>
    (e: ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
      setBilling({ ...billing, [k]: e.target.value });

  const handleCardChange =
    (k: keyof CardInfo) =>
    (e: ChangeEvent<HTMLInputElement>) =>
      setCard({ ...card, [k]: e.target.value });

  return (
    <div className="min-h-screen bg-gradient-to-br from-[#f8fafc] via-[#e0e7ff] to-[#f0f5ff] dark:from-[#181c2b] dark:via-[#23263a] dark:to-[#1a1d2b]">
      {/* Header */}
      <header className="py-12 px-4 md:px-0 text-center">
        <h1 className="text-4xl md:text-5xl font-extrabold text-indigo-700 dark:text-pink-400 mb-3 drop-shadow">
          Billing & Payment
        </h1>
        <p className="text-lg md:text-xl text-slate-600 dark:text-slate-300 font-medium">
          Complete your purchase securely and easily
        </p>
      </header>

      {/* Main Content */}
      <main className="max-w-6xl mx-auto px-4 md:px-0 pb-16">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-10">
          {/* Left: Forms */}
          <div className="lg:col-span-2 space-y-10">
            {/* Billing Details */}
            <section className="rounded-2xl bg-white dark:bg-stone-900 p-10 shadow-xl border border-slate-100 dark:border-stone-800">
              <h2 className="text-xl font-bold text-indigo-700 dark:text-pink-400 mb-2">
                Contact & Address
              </h2>
              <p className="text-base text-slate-500 dark:text-slate-400 mb-6">
                Enter your billing address
              </p>
              <form className="grid grid-cols-1 sm:grid-cols-2 gap-8">
                <label className="col-span-1 sm:col-span-2">
                  <span className="block mb-2 text-base font-medium text-slate-700 dark:text-slate-300">Full Name</span>
                  <input
                    value={billing.name}
                    onChange={handleBillingChange("name")}
                    className="w-full rounded-lg border border-indigo-200 dark:border-pink-400 bg-white dark:bg-stone-800 px-4 py-3 text-base shadow-sm placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-400 dark:focus:ring-pink-400 transition"
                  />
                </label>
                <label className="col-span-1 sm:col-span-2">
                  <span className="block mb-2 text-base font-medium text-slate-700 dark:text-slate-300">Email Address</span>
                  <input
                    type="email"
                    value={billing.email}
                    onChange={handleBillingChange("email")}
                    className="w-full rounded-lg border border-indigo-200 dark:border-pink-400 bg-white dark:bg-stone-800 px-4 py-3 text-base shadow-sm placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-400 dark:focus:ring-pink-400 transition"
                  />
                </label>
                <label className="col-span-1 sm:col-span-2">
                  <span className="block mb-2 text-base font-medium text-slate-700 dark:text-slate-300">Address</span>
                  <input
                    value={billing.address}
                    onChange={handleBillingChange("address")}
                    className="w-full rounded-lg border border-indigo-200 dark:border-pink-400 bg-white dark:bg-stone-800 px-4 py-3 text-base shadow-sm placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-400 dark:focus:ring-pink-400 transition"
                  />
                </label>
                <label>
                  <span className="block mb-2 text-base font-medium text-slate-700 dark:text-slate-300">City</span>
                  <input
                    value={billing.city}
                    onChange={handleBillingChange("city")}
                    className="w-full rounded-lg border border-indigo-200 dark:border-pink-400 bg-white dark:bg-stone-800 px-4 py-3 text-base shadow-sm placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-400 dark:focus:ring-pink-400 transition"
                  />
                </label>
                <label>
                  <span className="block mb-2 text-base font-medium text-slate-700 dark:text-slate-300">ZIP / Postal</span>
                  <input
                    value={billing.zip}
                    onChange={handleBillingChange("zip")}
                    className="w-full rounded-lg border border-indigo-200 dark:border-pink-400 bg-white dark:bg-stone-800 px-4 py-3 text-base shadow-sm placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-400 dark:focus:ring-pink-400 transition"
                  />
                </label>
                <label className="col-span-1 sm:col-span-2">
                  <span className="block mb-2 text-base font-medium text-slate-700 dark:text-slate-300">Country</span>
                  <select
                    value={billing.country}
                    onChange={handleBillingChange("country")}
                    className="w-full rounded-lg border border-indigo-200 dark:border-pink-400 bg-white dark:bg-stone-800 px-4 py-3 text-base shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-400 dark:focus:ring-pink-400 transition"
                  >
                    <option>Bangladesh</option>
                    <option>United States</option>
                    <option>United Kingdom</option>
                    <option>India</option>
                  </select>
                </label>
              </form>
            </section>

            {/* Card Payment */}
            <section className="rounded-2xl bg-white dark:bg-stone-900 p-10 shadow-xl border border-slate-100 dark:border-stone-800">
              <div className="flex items-center justify-between mb-6">
                <div>
                  <h3 className="text-xl font-bold text-indigo-700 dark:text-pink-400">Payment</h3>
                  <p className="text-base text-slate-500 dark:text-slate-400">We accept major credit cards & mobile payments</p>
                </div>
                <div className="text-base text-green-500 flex items-center gap-1 font-semibold">
                  <span className="text-xl">🔒</span> Secure
                </div>
              </div>
              <form className="grid grid-cols-1 sm:grid-cols-2 gap-8">
                <label className="col-span-1 sm:col-span-2">
                  <span className="block mb-2 text-base font-medium text-slate-700 dark:text-slate-300">Card Number</span>
                  <input
                    placeholder="4242 4242 4242 4242"
                    value={card.number}
                    onChange={handleCardChange("number")}
                    className="w-full rounded-lg border border-indigo-200 dark:border-pink-400 bg-white dark:bg-stone-800 px-4 py-3 text-base shadow-sm placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-400 dark:focus:ring-pink-400 transition"
                  />
                </label>
                <label>
                  <span className="block mb-2 text-base font-medium text-slate-700 dark:text-slate-300">Name on Card</span>
                  <input
                    value={card.name}
                    onChange={handleCardChange("name")}
                    className="w-full rounded-lg border border-indigo-200 dark:border-pink-400 bg-white dark:bg-stone-800 px-4 py-3 text-base shadow-sm placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-400 dark:focus:ring-pink-400 transition"
                  />
                </label>
                <label>
                  <span className="block mb-2 text-base font-medium text-slate-700 dark:text-slate-300">Expiry</span>
                  <input
                    placeholder="MM/YY"
                    value={card.expiry}
                    onChange={handleCardChange("expiry")}
                    className="w-full rounded-lg border border-indigo-200 dark:border-pink-400 bg-white dark:bg-stone-800 px-4 py-3 text-base shadow-sm placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-400 dark:focus:ring-pink-400 transition"
                  />
                </label>
                <label>
                  <span className="block mb-2 text-base font-medium text-slate-700 dark:text-slate-300">CVC</span>
                  <input
                    placeholder="123"
                    value={card.cvc}
                    onChange={handleCardChange("cvc")}
                    className="w-full rounded-lg border border-indigo-200 dark:border-pink-400 bg-white dark:bg-stone-800 px-4 py-3 text-base shadow-sm placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-400 dark:focus:ring-pink-400 transition"
                  />
                </label>
              </form>
              <div className="mt-8 flex items-center gap-3">
                <button className="inline-flex items-center gap-2 rounded-lg border border-indigo-200 dark:border-pink-400 bg-white/70 dark:bg-stone-800/70 px-5 py-2 text-base font-medium shadow hover:bg-indigo-50 dark:hover:bg-pink-950 transition">
                  <span className="text-lg">➕</span> Add another payment method
                </button>
                <button className="inline-flex items-center rounded-lg bg-gradient-to-r from-indigo-500 via-purple-500 to-pink-500 px-8 py-3 text-base font-semibold text-white shadow-lg hover:scale-105 transition">
                  <span className="text-lg">💳</span> Pay now
                </button>
              </div>
            </section>
          </div>

          {/* Right: Summary + Payment Methods + Shipping */}
          <aside className="space-y-10">
            {/* Order Summary */}
            <div className="rounded-2xl bg-white dark:bg-stone-900 p-10 shadow-xl border border-slate-100 dark:border-stone-800">
              <h3 className="text-xl font-bold text-indigo-700 dark:text-pink-400 mb-2">Order Summary</h3>
              {plan ? (
                <>
                  <div className="mb-2 text-lg font-semibold text-slate-700 dark:text-slate-200">{plan.title}</div>
                  <div className="mb-2 text-base text-slate-500 dark:text-slate-400">{plan.description}</div>
                  <div className="mb-4 text-base text-indigo-700 dark:text-pink-400 font-bold">{plan.subtitle}</div>
                  <ul className="mb-4 list-disc pl-5 text-sm text-slate-600 dark:text-slate-300">
                    {plan.features.map((f, i) => (
                      <li key={i}>{f}</li>
                    ))}
                  </ul>
                  <div className="mt-4 text-lg font-bold text-indigo-700 dark:text-pink-400">
                    Total: ${price}
                  </div>
                </>
              ) : (
                <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
  {/* Title + Price */}
  <div className="mb-2 flex items-center justify-between">
    <h3 className="text-lg font-semibold text-slate-900">Basic plan</h3>
    <span className="text-indigo-600 font-medium">$10/mth</span>
  </div>

  {/* Description */}
  <p className="text-sm text-slate-500 mb-4">Billed annually.</p>

  {/* Features */}
  <ul className="space-y-2 text-sm text-slate-600">
    <li>✓ Access to all basic features</li>
    <li>✓ Basic reporting and analytics</li>
    <li>✓ Up to 10 individual users</li>
    <li>✓ 20 GB individual data</li>
    <li>✓ Basic chat and email support</li>
  </ul>
</div>

              )}
            </div>

            {/* Payment Methods */}
            <div className="rounded-3xl bg-white dark:bg-stone-900 p-6 shadow-xl border border-slate-100 dark:border-stone-800 text-sm text-slate-500 dark:text-slate-400">
              <div className="font-medium text-slate-700 dark:text-slate-200 mb-4">Payment Methods</div>
              <div className="grid grid-cols-3 gap-4">
                {[
                  { name: "Visa", src: "https://upload.wikimedia.org/wikipedia/commons/thumb/1/16/Former_Visa_%28company%29_logo.svg/250px-Former_Visa_%28company%29_logo.svg.png" },
                  { name: "MasterCard", src: "https://upload.wikimedia.org/wikipedia/commons/thumb/2/2a/Mastercard-logo.svg/2560px-Mastercard-logo.svg.png" },
                  { name: "AmEx", src: "https://logos-world.net/wp-content/uploads/2020/11/American-Express-Logo.png" },
                  { name: "bKash", src: "https://www.tbsnews.net/sites/default/files/styles/big_3/public/images/2024/09/12/3ede081fe791711d1ebd24e5d5072e169ff089785d251345.jpg" },
                  { name: "Rocket", src: "https://images.seeklogo.com/logo-png/31/2/dutch-bangla-rocket-logo-png_seeklogo-317692.png" },
                  { name: "Nagad", src: "https://play-lh.googleusercontent.com/9ps_d6nGKQzfbsJfMaFR0RkdwzEdbZV53ReYCS09Eo5MV-GtVylFD-7IHcVktlnz9Mo" },
                  { name: "Upay", src: "https://today.thefinancialexpress.com.bd/public/uploads/p9-Upay-Logo.jpg" },
                ].map((m) => (
                  <div key={m.name} className="flex items-center justify-center rounded-xl border border-slate-200 dark:border-stone-700 bg-white dark:bg-stone-800 p-3 shadow hover:scale-105 transform transition">
                    <img src={m.src} alt={m.name} className="h-6 w-auto" />
                  </div>
                ))}
              </div>
            </div>

            {/* Shipping Address */}
            <div className="rounded-2xl bg-white dark:bg-stone-900 p-8 shadow-xl border border-slate-100 dark:border-stone-800 text-base text-slate-500 dark:text-slate-400">
              <div className="font-semibold text-indigo-700 dark:text-pink-400 mb-2">Shipping Address</div>
              <div className="mt-2">{billing.address}, {billing.city} — {billing.zip}</div>
            </div>
          </aside>
        </div>
      </main>

      {/* Footer */}
      <footer className="mt-16 text-center text-base text-indigo-400 dark:text-pink-400 font-semibold">
        © 2025 Cutypai — All rights reserved
      </footer>
    </div>
  );
}

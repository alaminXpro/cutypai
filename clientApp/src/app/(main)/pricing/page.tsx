"use client";

import React, { useState, ChangeEvent, FC } from "react";
import { LayersThree01, LayersTwo01, Zap } from "@untitledui/icons";
import { useRouter } from "next/navigation";

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

type PricingTierProps = {
  title: string;
  subtitle: string;
  description: string;
  features: string[];
  icon: FC<{ className?: string }>;
  onChoose?: () => void; // ← new prop
};

// ---------- Pricing Card ----------
const PricingTierCardIcon: FC<PricingTierProps> = ({
  title,
  subtitle,
  description,
  features,
  icon: Icon,
  onChoose,
}) => (
  <div className="flex flex-col rounded-2xl border border-slate-200 bg-white p-6 shadow-sm hover:shadow-md transition dark:bg-stone-900 dark:border-stone-700">
    <div className="flex items-center gap-3">
      <Icon className="h-8 w-8 text-indigo-600" />
      <div>
        <h3 className="text-lg font-semibold text-slate-900 dark:text-white">{title}</h3>
        <p className="text-sm text-slate-500">{subtitle}</p>
      </div>
    </div>
    <p className="mt-4 text-sm text-slate-600 dark:text-slate-400">{description}</p>
    <ul className="mt-6 space-y-2 text-sm text-slate-600 dark:text-slate-300">
      {features.map((f) => (
        <li key={f} className="flex items-center gap-2">
          <span className="text-green-600">✓</span>
          {f}
        </li>
      ))}
    </ul>
    <button
      onClick={onChoose} // ← navigation callback
      className="mt-6 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 transition"
    >
      Choose plan
    </button>
  </div>
);

// ---------- Plans ----------
const plans: PricingTierProps[] = [
  {
    title: "Basic plan",
    subtitle: "$10/mth",
    description: "Billed annually.",
    features: [
      "Access to all basic features",
      "Basic reporting and analytics",
      "Up to 10 individual users",
      "20 GB individual data",
      "Basic chat and email support",
    ],
    icon: Zap,
  },
  {
    title: "Business plan",
    subtitle: "$20/mth",
    description: "Billed annually.",
    features: [
      "200+ integrations",
      "Advanced reporting and analytics",
      "Up to 20 individual users",
      "40 GB individual data",
      "Priority chat and email support",
    ],
    icon: LayersTwo01,
  },
  {
    title: "Enterprise plan",
    subtitle: "$40/mth",
    description: "Billed annually.",
    features: [
      "Advanced custom fields",
      "Audit log and data history",
      "Unlimited individual users",
      "Unlimited individual data",
      "Personalized + priority service",
    ],
    icon: LayersThree01,
  },
];

// ---------- Main Billing Page ----------
export default function BillingPage() {
  const router = useRouter(); // ← useRouter hook

  const [billing, setBilling] = useState<BillingInfo>({
    name: "John Doe",
    email: "john@example.com",
    address: "123 Ocean Ave",
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

  // ---------- Navigation function ----------
  const goToBillingDetails = (plan: PricingTierProps) => {
    const planData = encodeURIComponent(
      JSON.stringify({
        title: plan.title,
        subtitle: plan.subtitle,
        description: plan.description,
        features: plan.features,
      })
    );
    router.push(`/billing_details?plan=${planData}`);
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-slate-100 to-slate-200 dark:from-stone-950 dark:via-stone-900 dark:to-stone-950 p-6">
      <div className="max-w-6xl mx-auto">
        {/* ---------- Pricing Section ---------- */}
        <section className="mb-16">
          <div className="relative text-center max-w-3xl mx-auto px-4">
            <p className="text-sm sm:text-base font-semibold text-indigo-500 uppercase tracking-wider">
              Pricing
            </p>
            <h2 className="mt-4 text-5xl sm:text-6xl md:text-7xl font-extrabold text-slate-900 dark:text-white relative inline-block">
              Simple, transparent pricing
              <span className="absolute -inset-1 -z-10 rounded-xl bg-gradient-to-r from-indigo-200 via-purple-200 to-pink-200 opacity-30 dark:opacity-20"></span>
            </h2>
            <p className="mt-6 text-lg sm:text-xl md:text-2xl text-slate-600 dark:text-slate-400 leading-relaxed max-w-2xl mx-auto">
              We believe Untitled should be accessible to all companies, no matter the size.
            </p>
            <div className="mt-8 flex justify-center gap-2">
              <span className="h-1 w-12 rounded-full bg-indigo-400 opacity-50 animate-pulse"></span>
              <span className="h-1 w-8 rounded-full bg-purple-400 opacity-50 animate-pulse delay-150"></span>
              <span className="h-1 w-6 rounded-full bg-pink-400 opacity-50 animate-pulse delay-300"></span>
            </div>
          </div>

          <div className="mt-12 grid grid-cols-1 gap-6 md:grid-cols-2 xl:grid-cols-3">
            {plans.map((plan) => (
              <PricingTierCardIcon
                key={plan.title}
                {...plan}
                onChoose={() => goToBillingDetails(plan)}
              />
            ))}
          </div>
        </section>
      </div>
    </div>
  );
}

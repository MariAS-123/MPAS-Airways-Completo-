/** @type {import('tailwindcss').Config} */
export default {
  content: [
    './index.html',
    './src/**/*.{vue,js,ts,jsx,tsx}',
  ],
  theme: {
    extend: {
      colors: {
        navy:         '#0B1F3B',
        'navy-light': '#123A6B',
        'blue-accent':'#1F5FA5',
        gold:         '#D4A63A',
        'gold-light': '#F2C94C',
        'gold-dark':  '#B88A2A',
        background:   '#F5F7FA',
        border:       '#E5E7EB',
        'text-muted': '#6B7280',
        'text-main':  '#111827',
        success:      '#22C55E',
        error:        '#EF4444',
        warning:      '#F59E0B',
      },
    },
  },
  plugins: [],
}

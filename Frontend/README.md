# Safarni — Angular 21 Port

تحويل مباشر (1:1) لتصميم الصفحات الأربع الأصلية (Explore / Planner / Bookings / Concierge)
من HTML ثابت إلى تطبيق Angular 21 (standalone components + Router)، مع إصلاح الفلو بين الصفحات.

## البنية

```
src/
  index.html            # نفس Tailwind CDN + Google Fonts + إعدادات الثيم (مدمجة من الصفحات الأربع)
  styles.css            # كل الـ CSS المخصص من الصفحات الأربع (glass-card, timeline-line, ai-gradient...)
  app/
    app.routes.ts        # جدول الراوتس: '', 'planner', 'bookings', 'concierge'
    app.config.ts
    app.component.ts     # <router-outlet/>
    pages/
      explore/            # الصفحة الرئيسية (Discover Your Next Journey)
      planner/             # صفحة الـ Itinerary (7-day Tokyo Adventure)
      bookings/            # صفحة الحجوزات
      concierge/           # شات الـ AI Concierge (بمنطق إرسال/رد فعلي)
```

## اللي اتغيّر فعليًا (الفلو فقط، مفيش تغيير في التصميم أو المحتوى)

1. **الراوتينج**: كل روابط الناف بار (Explore / Planner / Bookings / Concierge) في الهيدر
   والـ Bottom Nav في الصفحات الأربع كانت `href="#"` (مفيش فلو حقيقي) → دلوقتي `routerLink`
   بيوديك للصفحة الفعلية، والعنصر النشط بيتلوّن أوتوماتيك عن طريق `routerLinkActive`
   (بدل ما كان الكلاس "active" متسطّر يدويًا في كل صفحة).
2. **أزرار الدخول لشات الـ AI** اللي كانت رايحة لـ `#` (زرار "Chat with AI" في الهوم،
   الفقاعة العائمة، وزرار "Refine with AI" في صفحة الـ Planner، وزرار "Ask Concierge"
   في صفحة الحجوزات) → دلوقتي فعليًا بتوديك لصفحة `/concierge`.
3. **شات الـ Concierge**: منطق الإرسال ومؤشر الكتابة (typing indicator) والرد التلقائي
   بعد 1.5 ثانية اتحوّل من الـ vanilla JS الأصلي لمنطق Angular حقيقي (`(click)`, `(keyup.enter)`,
   `[(ngModel)]`, و control flow `@for` / `@if`) — بنفس السلوك والتوقيت بالظبط.
4. سكريبتات الـ micro-interactions اللي كانت فاضية (مجرد `addEventListener` من غير أي منطق
   جواها) اتشالت، وسكريبت تظليل الهيدر عند الـ scroll في صفحة الـ Planner اتحول لـ
   `HostListener('window:scroll')`.
5. زرار "Profile" في كل الصفحات سايبه زي ما هو (`href="#"`) لأنه مفيش صفحة بروفايل
   في التصميم الأصلي أصلاً.

## تشغيل المشروع

```bash
npm install
npm start
```

هيفتح على `http://localhost:4200`.

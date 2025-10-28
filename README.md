
# Encuesta App — Expo + Supabase (Starter)

Este starter te guía para levantar **rápido** una app móvil (Android/iOS) con **Expo** que guarda encuestas (texto + foto) en **Supabase** (gratis).

## Requisitos
- Node.js 18+
- Cuenta en Supabase (plan gratuito)
- App **Expo Go** instalada en tu celular

## 1) Crear proyecto con Expo
```bash
npx create-expo-app encuesta-app
cd encuesta-app
npm i @supabase/supabase-js expo-image-picker
```

## 2) Añadir archivos de este starter
Copia **App.js** y **supabase.js** de este starter dentro de tu carpeta `encuesta-app/` (sobrescribe `App.js`).

## 3) Configurar Supabase
- Entra a tu proyecto de Supabase y copia:
  - Project URL (ej. `https://xxxxx.supabase.co`)
  - anon public key
- En **Storage** crea un bucket **photos** y márcalo **public**.
- En **SQL Editor** pega el contenido de `survey_schema.sql` y ejecútalo.

## 4) Configurar claves en la app
Edita `supabase.js` y cambia `SUPABASE_URL` y `SUPABASE_ANON_KEY` por los tuyos.

## 5) Ejecutar
```bash
npx expo start
```
Escanea el QR con **Expo Go**, llena el formulario, toma una foto y **Enviar**.

## 6) Exportar datos
En Supabase → Table Editor → `survey_responses` → **Export** → CSV/Excel.

## Seguridad
Este demo permite INSERT anónimo para simplificar. En producción, cambia la policy a `authenticated` y agrega login (Auth).
```sql
drop policy if exists allow_insert_anon on public.survey_responses;
create policy "allow_insert_auth"
  on public.survey_responses
  for insert to authenticated
  with check (true);
```

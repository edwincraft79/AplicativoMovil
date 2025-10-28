
-- Tabla para respuestas de encuesta
create table if not exists public.survey_responses (
  id uuid primary key default gen_random_uuid(),
  created_at timestamptz default now(),
  nombre text,
  edad int,
  comentarios text,
  photo_path text,
  photo_url text
);

alter table public.survey_responses enable row level security;

-- Política de inserción anónima (para demo)
create policy if not exists "allow_insert_anon"
  on public.survey_responses
  for insert
  to anon
  with check (true);

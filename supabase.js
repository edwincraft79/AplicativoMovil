
import { createClient } from '@supabase/supabase-js'

// Reemplaza con tus valores de Supabase
export const SUPABASE_URL = 'https://TU-PROYECTO.supabase.co'
export const SUPABASE_ANON_KEY = 'TU-CLAVE-ANON'

export const supabase = createClient(SUPABASE_URL, SUPABASE_ANON_KEY)

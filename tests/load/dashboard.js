// Prueba de carga del dashboard (AC-14.3): p95 < 800 ms con volumen real.
//
// Variables opcionales: VUS (usuarios por escenario, 10), DURACION ("30s") y PENSAR (segundos entre solicitudes, 1).
//
//   k6 run -e BASE_URL=http://localhost:8080 -e ADMIN_CORREO=admin@institucion.edu -e ADMIN_PASSWORD=... tests/load/dashboard.js
//
// Para medir con 10 000 tesis cargue antes los datos de prueba (scripts/load/seed-10000.sql) en una base
// DESECHABLE; nunca contra producción.
import http from 'k6/http';
import { check, fail, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const VUS = parseInt(__ENV.VUS || '10', 10);
const DURACION = __ENV.DURACION || '30s';
// Tiempo de "lectura" entre solicitudes de un mismo usuario (s). Con 0 se mide saturación, no uso real.
const PENSAR = parseFloat(__ENV.PENSAR || '1');

export const options = {
  scenarios: {
    kpis: { executor: 'constant-vus', vus: VUS, duration: DURACION, exec: 'kpis' },
    cola: { executor: 'constant-vus', vus: VUS, duration: DURACION, exec: 'cola' },
  },
  thresholds: {
    'http_req_duration{endpoint:kpis}': ['p(95)<800'],
    'http_req_duration{endpoint:cola}': ['p(95)<800'],
    http_req_failed: ['rate<0.01'],
    checks: ['rate>0.99'],
  },
};

export function setup() {
  const respuesta = http.post(
    `${BASE_URL}/api/v1/auth/login`,
    JSON.stringify({ correo: __ENV.ADMIN_CORREO, password: __ENV.ADMIN_PASSWORD }),
    { headers: { 'Content-Type': 'application/json' } },
  );

  if (respuesta.status !== 200) {
    fail(`No se pudo iniciar sesión (HTTP ${respuesta.status}). Verifique ADMIN_CORREO y ADMIN_PASSWORD.`);
  }

  return { token: respuesta.json('accessToken') };
}

function autenticado(datos, endpoint) {
  return { headers: { Authorization: `Bearer ${datos.token}` }, tags: { endpoint } };
}

export function kpis(datos) {
  const r = http.get(`${BASE_URL}/api/v1/dashboard/kpis`, autenticado(datos, 'kpis'));
  check(r, { 'kpis 200': (x) => x.status === 200 });
  sleep(PENSAR);
}

export function cola(datos) {
  const r = http.get(`${BASE_URL}/api/v1/dashboard/cola-validacion?pageSize=100`, autenticado(datos, 'cola'));
  check(r, { 'cola 200': (x) => x.status === 200 });
  sleep(PENSAR);
}

import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  scenarios: {
    steady_search_load: {
      executor: 'constant-vus',
      vus: 10,
      duration: '1m',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<2000'],
  },
};

const baseUrl = __ENV.BASE_URL || 'http://localhost:5164';
const queries = [
  'hello world',
  'scalability optimization',
  'classic song',
  'search engine',
];

export default function () {
  const query = queries[Math.floor(Math.random() * queries.length)];
  const response = http.post(
    `${baseUrl}/api/search`,
    JSON.stringify({ query }),
    { headers: { 'Content-Type': 'application/json' } },
  );

  check(response, {
    'status is 200': (res) => res.status === 200,
    'has provider results': (res) => {
      const body = res.json();
      return Array.isArray(body.providers) && body.providers.length >= 2;
    },
  });

  sleep(1);
}

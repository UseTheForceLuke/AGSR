FHIR date search handels timezone, percision up to minutes, for MVP i decided to not support seconds hence milisecodns range percision.<br>

yyyy-mm-ddThh:mm:ss.ssss[Z|(+|-)hh:mm]<br>

unicode + sign in query: %2B<br>
supports "eq", "ne", "gt", "lt", "ge", "le", "sa", "eb", "ap"<br>

how to run: docker-compose up -d --build<br>
to rerun fhir-seeder-cli - just start a container again with it to seed 100 patients, on a docker-compose up -d --build command it will seed after web-api is healthy, to delete seed execute postgres sql: truncate public.patients /patients<br>
swagger<br>
https://localhost:5001/swagger/index.html<br>
http://localhost:5000/swagger/index.html<br>

some examples:<br>

POST /patients<br>
where birthDate:<br>
2004-07-05T09:39:02.372+05:00 year<br>
2004-08-05T09:39:02.372+05:00<br>
<br>
2004-07-05T10:39:02.372+05:00 month<br>
2004-07-06T10:39:02.372+05:00<br>
<br>
2004-07-05T09:39:02.372+05:00 day<br>
2004-07-05T10:39:02.372+05:00<br>
<br>
2004-07-05T09:39:02.372+05:00 hour<br>
2004-07-05T09:40:02.372+05:00<br>
<br>
2004-07-05T09:39:02.372+05:00 minute<br>
2004-07-05T09:39:03.372+05:00<br>
<br>
GET<br>
eq<br>
/patients?date=eq2004-07-05T09:39:02.372%2B05:00<br>
/patients?date=eq2004 year<br>
/patients?date=eq2004-07 month<br>
/patients?date=eq2004-07-05 day<br>
/patients?date=eq2004-07-05T09%2B05:00 hour<br>
/patients?date=eq2004-07-05T09:39%2B05:00 minutes<br>

ge<br>
le<br>
/patients?date=ge2004-07-05T09:39%2B05:00&date=le2004-07-05T09:39%2B05:00<br>

Timezone adjsut example we get next day:<br>
POST /patients where birthDate 2004-07-05T10:40:05.373+14:00<br>
GET/patients/b95018d0-080a-4dc3-ac1a-cbc3bf1355c7<br>
        "birthDate": "2004-07-04T20:40:05.373+00:00",<br>
        "birthDateOffset": "14:00:00",<br>
        "originalBirthDate": "2004-07-05T10:40:05.373+14:00",<br>

FHIR date search handels timezone, percision up to minutes, for MVP i decided to not support seconds and milisecodns percision.

yyyy-mm-ddThh:mm:ss.ssss[Z|(+|-)hh:mm]<br>

unicode + sign in query: %2B<br>

Some examples:<br>

POST /patients<br>
where "birthDate":<br>
2004-07-05T09:39:02.372+05:00 year<br>
2004-08-05T09:39:02.372+05:00
<br>
2004-07-05T10:39:02.372+05:00 month<br>
2004-07-06T10:39:02.372+05:00
<br>
2004-07-05T09:39:02.372+05:00 day<br>
2004-07-05T10:39:02.372+05:00
<br>
2004-07-05T09:39:02.372+05:00 hour<br>
2004-07-05T09:40:02.372+05:00
<br>
2004-07-05T09:39:02.372+05:00 minute<br>
2004-07-05T09:39:03.372+05:00
<br>
GET<br>
eq
/patients?date=eq2004-07-05T09:39:02.372%2B05:00<br>
/patients?date=eq2004 year<br>
/patients?date=eq2004-07 month<br>
/patients?date=eq2004-07-05 day<br>
/patients?date=eq2004-07-05T09%2B05:00 hour<br>
/patients?date=eq2004-07-05T09:39%2B05:00 minutes<br>

/patients?date=ge2004-07-05T09:39%2B05:00&date=le2004-07-05T09:39%2B05:00<br>

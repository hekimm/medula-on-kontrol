#!/bin/bash
set -euo pipefail
runtime_password=$(cat /run/medula-secrets/runtime-password)
sqlplus -s /nolog <<SQL
set echo off verify off feedback off serveroutput on
whenever sqlerror exit sql.sqlcode rollback
connect MEDULA_RUN/"$runtime_password"@localhost:1521/XEPDB1
declare
  n number;
begin
  if user <> 'MEDULA_RUN' then raise_application_error(-20002,'Wrong runtime user'); end if;
  select count(*) into n from medula.kural;
  if n < 40 then raise_application_error(-20002,'Missing rules'); end if;
  select count(*) into n from (
    select f.donem from medula.fatura f join medula.basvuru b on b.id=f.basvuru_id
    join medula.fatura_kalemi k on k.fatura_id=f.id
    where b.kurum_id=1 group by f.donem having count(*)>=5000);
  if n < 3 then raise_application_error(-20002,'Three seeded periods are required'); end if;
  select count(distinct substr(kural_kodu,1,instr(kural_kodu,'-')-1)) into n from medula.bulgu;
  if n <> 8 then raise_application_error(-20002,'Missing violation category'); end if;
  select count(*) into n from medula.hasta where ad_soyad not like 'v1:%' or kimlik_no not like 'v1:%';
  if n <> 0 then raise_application_error(-20002,'Unencrypted patient fields'); end if;
  select count(*) into n from session_privs where privilege in ('ALTER ANY TRIGGER','DROP ANY TRIGGER','UPDATE ANY TABLE','DELETE ANY TABLE');
  if n <> 0 then raise_application_error(-20002,'Excessive runtime privileges'); end if;
  begin
    execute immediate 'UPDATE MEDULA.DENETIM_IZI SET VARLIK_ID=''test'' WHERE ID=-1';
    raise_application_error(-20002,'Denetim update was allowed');
  exception when others then
    if sqlcode not in (-1031,-20001) then raise; end if;
  end;
  begin
    execute immediate 'DELETE FROM MEDULA.DENETIM_IZI WHERE ID=-1';
    raise_application_error(-20002,'Denetim delete was allowed');
  exception when others then
    if sqlcode not in (-1031,-20001) then raise; end if;
  end;
  dbms_output.put_line('PASS: MEDULA_RUN reads rules; audit UPDATE/DELETE and owner DDL privileges denied.');
end;
/
select 'SEED_LINES=' || count(*) from medula.fatura_kalemi;
select 'RULE_CATEGORIES=' || count(distinct kategori) from medula.kural;
select 'FINDING_CATEGORIES=' || count(distinct substr(kural_kodu,1,instr(kural_kodu,'-')-1)) from medula.bulgu;
select 'ENCRYPTED_PATIENTS=' || count(*) from medula.hasta where ad_soyad like 'v1:%' and kimlik_no like 'v1:%';
select 'COMPLETED_JOBS=' || count(*) from medula.denetim_isi where durum='TAMAMLANDI';
select 'PERIOD=' || f.donem || ', LINES=' || count(*) from medula.fatura f
join medula.basvuru b on b.id=f.basvuru_id join medula.fatura_kalemi k on k.fatura_id=f.id
where b.kurum_id=1 group by f.donem order by f.donem;
exit
SQL

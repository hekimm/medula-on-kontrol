#!/bin/bash
set -euo pipefail
existing=$(sqlplus -s / as sysdba <<'SQL'
set heading off feedback off pagesize 0 verify off echo off
alter session set container=XEPDB1;
select count(*) from dba_users where username='MEDULA_RUN';
exit
SQL
)
if [[ "$(echo "$existing" | tr -d '[:space:]')" != "1" ]]; then
  createAppUser MEDULA_RUN "$(cat /run/medula-secrets/runtime-password)"
fi

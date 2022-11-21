#!/bin/bash
set -e

if [ $# -ne 1 ]; then
  echo This script expects only one parameter : the service to restart
  exit 1
fi

SERVICE=$1

healthcheck() {
  curl -fsSL localhost:5011/liveness
  curl -fsSL localhost:5011/startup
  
  curl -fsSL localhost:9980/liveness
  curl -fsSL localhost:9980/startup
  curl -fsSL localhost:9980/readiness
}

healthcheck
echo

docker-compose -f docker-compose/docker-compose.yml -f docker-compose/docker-compose.queue-activemqp.yml stop $SERVICE

for i in $(seq 0 2);
do
  healthcheck || true
  echo
  sleep 1
done

docker-compose -f docker-compose/docker-compose.yml -f docker-compose/docker-compose.queue-activemqp.yml restart $SERVICE

healthcheck || true
sleep 1

# If we restart the queue, wait for it to be active again
if [ "$SERVICE" = queue ];
then
  for i in $(seq 5);
  do
    if curl -f -u admin:admin -s http://localhost:8161/api/jolokia/exec/org.apache.activemq:type=Broker,brokerName=localhost,service=Health/healthStatus -H Origin:http://localhost |
      grep Good &>/dev/null;
    then
      break
    fi
    sleep 2
  done
fi

healthcheck || true

sleep 1

docker-compose -f docker-compose/docker-compose.yml -f docker-compose/docker-compose.queue-activemqp.yml restart armonik.control.submitter
docker-compose -f docker-compose/docker-compose.yml -f docker-compose/docker-compose.queue-activemqp.yml restart armonik.compute.pollingagent0
docker-compose -f docker-compose/docker-compose.yml -f docker-compose/docker-compose.queue-activemqp.yml restart armonik.compute.pollingagent1
docker-compose -f docker-compose/docker-compose.yml -f docker-compose/docker-compose.queue-activemqp.yml restart armonik.compute.pollingagent2

for i in $(seq 0 2);
do
  sleep 1
  healthcheck || true
  echo
done

healthcheck
echo

cd docker 
docker-compose down
docker images purge -a
docker system prune
docker volume prune -f
pause
exit
docker-compose down
docker images purge -a
docker container prune -f
docker system prune -f
pause
exit
docker-compose down
docker image prune -a -f
docker container prune -f
docker system prune -f
pause
exit
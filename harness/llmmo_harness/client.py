import httpx


class GameApiError(Exception):
    def __init__(self, status_code: int, message: str, body: dict | str | None = None):
        super().__init__(message)
        self.status_code = status_code
        self.body = body


class GameClient:
    def __init__(self, base_url: str, api_key: str) -> None:
        self.base_url = base_url.rstrip("/")
        self._headers = {
            "Authorization": f"Bearer {api_key}",
            "Content-Type": "application/json",
        }

    def _request(
        self,
        method: str,
        path: str,
        json_body: dict | None = None,
    ) -> httpx.Response:
        with httpx.Client(timeout=30.0) as client:
            response = client.request(
                method,
                f"{self.base_url}{path}",
                headers=self._headers,
                json=json_body,
            )
        return response

    def get_world(self) -> dict:
        response = self._request("GET", "/world")
        if response.status_code != 200:
            raise GameApiError(response.status_code, response.text)
        return response.json()

    def get_cities_me(self) -> list[dict]:
        response = self._request("GET", "/cities/me")
        if response.status_code != 200:
            raise GameApiError(response.status_code, response.text)
        return response.json()

    def get_troop_catalog(self) -> list[dict]:
        response = self._request("GET", "/catalog/troops")
        if response.status_code != 200:
            raise GameApiError(response.status_code, response.text)
        return response.json()

    def get_building_catalog(self) -> list[dict]:
        response = self._request("GET", "/catalog/buildings")
        if response.status_code != 200:
            raise GameApiError(response.status_code, response.text)
        return response.json()

    def create_action(self, city_id: str, action_type: str, payload: dict) -> dict:
        body = {
            "cityId": city_id,
            "type": action_type,
            "payload": payload,
        }
        response = self._request("POST", "/actions", json_body=body)
        if response.status_code == 201:
            return response.json()
        error_body: dict | str
        try:
            error_body = response.json()
            message = error_body.get("error", response.text)
        except Exception:
            error_body = response.text
            message = response.text
        raise GameApiError(response.status_code, message, error_body)
